// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using GuidelineModelIO;
using GuidelineService.Api.Models;
using GuidelineService.Api.Options;
using GuidelineService.Api.Repositories.Interfaces;
using GuidelineService.Api.Validation;
using GuidelineService.Events;
using Microsoft.Extensions.Options;

namespace GuidelineService.Api.Services;

/// <summary>
/// The result of a guideline create or update operation.
/// </summary>
public class GuidelineOperationResult
{
	/// <summary>Gets a value indicating whether the operation succeeded.</summary>
	public bool Success
	{
		get; init;
	}

	/// <summary>Gets a value indicating whether the target guideline was not found.</summary>
	public bool NotFound
	{
		get; init;
	}

	/// <summary>Gets the error message when the operation failed validation.</summary>
	public string? Error
	{
		get; init;
	}

	/// <summary>Gets the resulting guideline metadata on success.</summary>
	public GuidelineMetadata? Guideline
	{
		get; init;
	}

	/// <summary>Creates a successful result wrapping the given guideline.</summary>
	public static GuidelineOperationResult Ok(GuidelineMetadata guideline) => new() { Success = true, Guideline = guideline };

	/// <summary>Creates a validation-failure result with the given message.</summary>
	public static GuidelineOperationResult Invalid(string error) => new() { Success = false, Error = error };

	/// <summary>Creates a not-found result.</summary>
	public static GuidelineOperationResult MissingGuideline() => new() { Success = false, NotFound = true };
}

/// <summary>
/// The downloadable content of a guideline file together with its metadata.
/// </summary>
/// <param name="Content">The file content stream.</param>
/// <param name="ContentType">The content type of the file.</param>
/// <param name="FileName">The original file name.</param>
public record GuidelineFileResult(Stream Content, string ContentType, string FileName);

/// <summary>
/// Orchestrates guideline lifecycle operations: validating the uploaded file, storing it in object
/// storage, persisting its metadata, and emitting an outbox event so downstream services are notified.
/// </summary>
public class GuidelineAppService
{
	private const string GuidelineContentType = "application/json";

	private readonly IGuidelineStorageRepository _storage;
	private readonly IGuidelineMetadataRepository _metadata;
	private readonly IOutboxRepository _outbox;
	private readonly GuidelineReaderWriter _readerWriter;
	private readonly ILogger<GuidelineAppService> _logger;
	private readonly string _bucket;
	private readonly string _topic;

	/// <summary>
	/// Initializes a new instance of the <see cref="GuidelineAppService"/> class.
	/// </summary>
	/// <param name="storage">The object storage repository for guideline files.</param>
	/// <param name="metadata">The metadata repository for guideline entries.</param>
	/// <param name="outbox">The outbox repository used to emit change events.</param>
	/// <param name="readerWriter">The guideline reader/writer used for deserialization.</param>
	/// <param name="minioOptions">The MinIO configuration options.</param>
	/// <param name="kafkaOptions">The Kafka configuration options.</param>
	/// <param name="logger">The logger.</param>
	public GuidelineAppService(
		IGuidelineStorageRepository storage,
		IGuidelineMetadataRepository metadata,
		IOutboxRepository outbox,
		GuidelineReaderWriter readerWriter,
		IOptions<MinioOptions> minioOptions,
		IOptions<KafkaOptions> kafkaOptions,
		ILogger<GuidelineAppService> logger)
	{
		_storage = storage;
		_metadata = metadata;
		_outbox = outbox;
		_readerWriter = readerWriter;
		_logger = logger;
		_bucket = minioOptions.Value.BucketName;
		_topic = !string.IsNullOrWhiteSpace(kafkaOptions.Value.UploadedGuidelineTopic)
			? kafkaOptions.Value.UploadedGuidelineTopic
			: throw new ArgumentException("Kafka:UploadedGuidelineTopic configuration is missing");
	}

	/// <summary>
	/// Returns metadata for all stored guidelines.
	/// </summary>
	public Task<List<GuidelineMetadata>> GetAllAsync(CancellationToken ct = default) => _metadata.GetAllAsync(ct);

	/// <summary>
	/// Returns metadata for a single guideline, or <c>null</c> if it does not exist.
	/// </summary>
	public Task<GuidelineMetadata?> GetByIdAsync(Guid id, CancellationToken ct = default) => _metadata.GetByIdAsync(id, ct);

	/// <summary>
	/// Downloads the raw guideline file together with its content type and file name.
	/// </summary>
	/// <returns>The file result, or <c>null</c> if the guideline does not exist.</returns>
	public async Task<GuidelineFileResult?> GetFileAsync(Guid id, CancellationToken ct = default)
	{
		var guideline = await _metadata.GetByIdAsync(id, ct);
		if (guideline is null)
			return null;

		var content = await _storage.GetAsync(guideline.Bucket, guideline.ObjectKey);
		return new GuidelineFileResult(content, guideline.ContentType, guideline.FileName);
	}

	/// <summary>
	/// Validates and stores a new guideline: uploads the file to object storage, persists its metadata,
	/// and emits an <see cref="UploadedGuideline"/> event. A new GUID is generated server-side.
	/// </summary>
	public async Task<GuidelineOperationResult> CreateAsync(IFormFile file, CancellationToken ct = default)
	{
		var validation = await ValidateAsync(file);
		if (validation is not null)
			return validation;

		var id = Guid.NewGuid();
		var objectKey = BuildObjectKey(id);
		var name = Path.GetFileNameWithoutExtension(file.FileName);

		await using var uploadStream = file.OpenReadStream();
		var response = await _storage.UploadAsync(_bucket, objectKey, uploadStream, GuidelineContentType, file.Length);

		var guideline = new GuidelineMetadata(id, name, file.FileName, GuidelineContentType,
			_bucket, objectKey, response.Etag, file.Length, DateTimeOffset.UtcNow);

		_metadata.Add(guideline);
		AddUploadedEvent(guideline);

		if (!await TryCommitAsync(ct))
		{
			await SafeRemoveAsync(_bucket, objectKey);
			return GuidelineOperationResult.Invalid("Failed to persist guideline metadata.");
		}

		return GuidelineOperationResult.Ok(guideline);
	}

	/// <summary>
	/// Replaces the file of an existing guideline. The metadata identity (<see cref="GuidelineMetadata.Id"/>
	/// and creation timestamp) is preserved while file-related fields are updated and an
	/// <see cref="UploadedGuideline"/> event is emitted so downstream services re-process the file.
	/// </summary>
	public async Task<GuidelineOperationResult> ReplaceFileAsync(Guid id, IFormFile file, CancellationToken ct = default)
	{
		var guideline = await _metadata.GetByIdAsync(id, ct);
		if (guideline is null)
			return GuidelineOperationResult.MissingGuideline();

		var validation = await ValidateAsync(file);
		if (validation is not null)
			return validation;

		var objectKey = BuildObjectKey(id);
		var name = Path.GetFileNameWithoutExtension(file.FileName);

		await using var uploadStream = file.OpenReadStream();
		var response = await _storage.UploadAsync(_bucket, objectKey, uploadStream, GuidelineContentType, file.Length);

		guideline.ReplaceFile(name, file.FileName, objectKey, response.Etag, file.Length, DateTimeOffset.UtcNow);
		AddUploadedEvent(guideline);

		if (!await TryCommitAsync(ct))
		{
			// The object key is deterministic from the id, so an update overwrites the previous object.
			// Nothing to roll back here beyond logging; the previous metadata row is still intact.
			return GuidelineOperationResult.Invalid("Failed to persist guideline metadata.");
		}

		return GuidelineOperationResult.Ok(guideline);
	}

	/// <summary>
	/// Deletes a guideline: removes its metadata, emits a <see cref="DeletedGuideline"/> event, and
	/// removes the underlying file from object storage.
	/// </summary>
	/// <returns><c>true</c> if the guideline existed and was deleted; otherwise <c>false</c>.</returns>
	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var guideline = await _metadata.GetByIdAsync(id, ct);
		if (guideline is null)
			return false;

		_metadata.Remove(guideline);
		_outbox.Add(new DeletedGuideline
		{
			Id = id.ToString(),
			BucketName = guideline.Bucket,
			ObjectKey = guideline.ObjectKey,
			Timestamp = DateTimeOffset.UtcNow
		}, _topic, id.ToString());

		await _metadata.SaveChangesAsync(ct);

		// Metadata and the delete event are committed; remove the file as a best-effort cleanup.
		await SafeRemoveAsync(guideline.Bucket, guideline.ObjectKey);

		return true;
	}

	/// <summary>
	/// Validates the uploaded file: presence, deserialization and structural validity.
	/// Returns a failure result, or <c>null</c> when the file is valid.
	/// </summary>
	private async Task<GuidelineOperationResult?> ValidateAsync(IFormFile file)
	{
		if (file == null || file.Length == 0)
			return GuidelineOperationResult.Invalid("File is null or empty.");

		string content;
		await using (var stream = file.OpenReadStream())
		using (var reader = new StreamReader(stream))
		{
			content = await reader.ReadToEndAsync();
		}

		Guideline.Model.Model.Guideline? guideline;
		try
		{
			guideline = _readerWriter.GetGuidelineFromString(content) as Guideline.Model.Model.Guideline;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Rejected guideline upload '{Name}': deserialization failed.", file.FileName);
			return GuidelineOperationResult.Invalid("Guideline could not be deserialized.");
		}

		if (guideline is null)
			return GuidelineOperationResult.Invalid("Guideline deserialization produced an invalid type.");

		var result = GuidelineStructureValidator.Validate(guideline);
		if (!result.IsValid)
			return GuidelineOperationResult.Invalid("Guideline validation failed: " + string.Join("; ", result.Errors));

		return null;
	}

	private void AddUploadedEvent(GuidelineMetadata guideline)
	{
		_outbox.Add(new UploadedGuideline
		{
			Id = guideline.Id.ToString(),
			Name = guideline.Name,
			ObjectKey = guideline.ObjectKey,
			Etag = guideline.Etag,
			BucketName = guideline.Bucket,
			CorrelationId = Guid.NewGuid(),
			Timestamp = DateTimeOffset.UtcNow
		}, _topic, guideline.Id.ToString());
	}

	private static string BuildObjectKey(Guid id) => $"{id}.guideline";

	private async Task<bool> TryCommitAsync(CancellationToken ct)
	{
		var saved = await _metadata.SaveChangesAsync(ct);
		return saved > 0;
	}

	private async Task SafeRemoveAsync(string bucket, string objectKey)
	{
		try
		{
			await _storage.RemoveAsync(bucket, objectKey);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to remove guideline object {ObjectKey} from bucket {Bucket}", objectKey, bucket);
		}
	}
}
