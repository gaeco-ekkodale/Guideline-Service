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
using GuidelineService.Api.Controllers;
using GuidelineService.Api.DTOs;
using GuidelineService.Api.Models;
using GuidelineService.Api.Options;
using GuidelineService.Api.Repositories.Interfaces;
using GuidelineService.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GuidelineService.Api.Tests.Controllers;

public class GuidelineControllerExtendedTests
{
	private readonly IGuidelineStorageRepository _storage;
	private readonly IGuidelineMetadataRepository _metadata;
	private readonly IOutboxRepository _outbox;
	private readonly GuidelineReaderWriter _readerWriter;
	private readonly ILogger<GuidelineAppService> _serviceLogger;
	private readonly GuidelineAppService _appService;
	private readonly ILogger<GuidelineController> _logger;
	private readonly GuidelineController _controller;

	public GuidelineControllerExtendedTests()
	{
		_storage = Substitute.For<IGuidelineStorageRepository>();
		_metadata = Substitute.For<IGuidelineMetadataRepository>();
		_outbox = Substitute.For<IOutboxRepository>();
		_readerWriter = Substitute.For<GuidelineReaderWriter>();
		_serviceLogger = Substitute.For<ILogger<GuidelineAppService>>();

		var minioOptions = Microsoft.Extensions.Options.Options.Create(new MinioOptions { BucketName = "guidelines" });
		var kafkaOptions = Microsoft.Extensions.Options.Options.Create(new KafkaOptions { UploadedGuidelineTopic = "guideline-topic" });

		_appService = new GuidelineAppService(
			_storage,
			_metadata,
			_outbox,
			_readerWriter,
			minioOptions,
			kafkaOptions,
			_serviceLogger);

		_logger = Substitute.For<ILogger<GuidelineController>>();
		_controller = new GuidelineController(_appService, _logger);
	}


	[Fact]
	public async Task GetGuidelines_ReturnsAllGuidelines()
	{
		var guidelines = new List<GuidelineMetadata>
		{
			new(Guid.NewGuid(), "name1", "name1.json", "application/json", "guidelines", "id1.json", "etag1", 100, DateTimeOffset.UtcNow),
			new(Guid.NewGuid(), "name2", "name2.json", "application/json", "guidelines", "id2.json", "etag2", 200, DateTimeOffset.UtcNow)
		};

		_metadata.GetAllAsync(CancellationToken.None)
			.Returns(guidelines);

		var result = await _controller.GetGuidelines(CancellationToken.None);

		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

		var dtos = Assert.IsAssignableFrom<IEnumerable<GuidelineDto>>(okResult.Value);
		Assert.Equal(2, dtos.Count());
	}

	[Fact]
	public async Task GetGuideline_WithExistingId_ReturnsGuideline()
	{
		var id = Guid.NewGuid();
		var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
			"guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

		_metadata.GetByIdAsync(id, CancellationToken.None)
			.Returns(guideline);

		var result = await _controller.GetGuideline(id, CancellationToken.None);

		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

		var dto = Assert.IsType<GuidelineDto>(okResult.Value);
		Assert.Equal(id, dto.Id);
		Assert.Equal("name", dto.Name);
	}

	[Fact]
	public async Task GetGuideline_WithNonExistentId_Returns404NotFound()
	{
		var id = Guid.NewGuid();

		_metadata.GetByIdAsync(id, CancellationToken.None)
			.Returns((GuidelineMetadata?)null);

		var result = await _controller.GetGuideline(id, CancellationToken.None);

		var notFoundResult = Assert.IsType<NotFoundResult>(result);
		Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
	}

	[Fact]
	public async Task GetGuidelineFile_WithExistingId_ReturnsFile()
	{
		var id = Guid.NewGuid();
		var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
			"guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

		var fileContent = "{}";
		var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

		_metadata.GetByIdAsync(id, CancellationToken.None)
			.Returns(guideline);

		_storage.GetAsync("guidelines", "id.json")
			.Returns(fileStream);

		var result = await _controller.GetGuidelineFile(id, CancellationToken.None);

		var fileActionResult = Assert.IsType<FileStreamResult>(result);
		Assert.Equal("application/json", fileActionResult.ContentType);
		Assert.Equal("name.json", fileActionResult.FileDownloadName);
	}

	[Fact]
	public async Task GetGuidelineFile_WithNonExistentId_Returns404NotFound()
	{
		var id = Guid.NewGuid();

		_metadata.GetByIdAsync(id, CancellationToken.None)
			.Returns((GuidelineMetadata?)null);

		// GetAsync should not be called when metadata returns null, but we must set up the mock to avoid leftover matchers
		_storage.GetAsync("guidelines", "")
			.Returns(new MemoryStream());

		var result = await _controller.GetGuidelineFile(id, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result);
	}


	[Fact]
	public async Task UpdateGuideline_WithNonExistentId_Returns404NotFound()
	{
		var id = Guid.NewGuid();
		var mockFile = CreateMockFormFile("new.json", "{}");

		_metadata.GetByIdAsync(id, CancellationToken.None)
			.Returns((GuidelineMetadata?)null);

		var result = await _controller.UpdateGuideline(id, mockFile, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result);
	}

	//[Fact(Skip = "GuidelineReaderWriter is not virtual, so mock setup doesn't work - would need interface extraction")]
	[Fact]
	public async Task UpdateGuideline_WithInvalidFile_Returns400BadRequest()
	{
		var id = Guid.NewGuid();
		var existingGuideline = new GuidelineMetadata(id, "old", "old.json", "application/json",
			"guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

		var mockFile = CreateMockFormFile("invalid.txt", "invalid");

		_metadata.GetByIdAsync(id, CancellationToken.None)
			.Returns(existingGuideline);

		// The real GetGuidelineFromString will be called since it's not virtual,
		// so we don't need to mock it - the invalid JSON will cause an exception

		var result = await _controller.UpdateGuideline(id, mockFile, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.NotNull(badRequest.Value);
	}

	[Fact]
	public async Task DeleteGuideline_WithExistingId_Returns204NoContent()
	{
		var id = Guid.NewGuid();
		var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
			"guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

		_metadata.GetByIdAsync(id, CancellationToken.None)
			.Returns(guideline);

		_metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
			.Returns(1);

		var result = await _controller.DeleteGuideline(id, CancellationToken.None);

		var noContentResult = Assert.IsType<NoContentResult>(result);
		Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
	}

	[Fact]
	public async Task DeleteGuideline_WithNonExistentId_Returns404NotFound()
	{
		var id = Guid.NewGuid();

		_metadata.GetByIdAsync(id, CancellationToken.None)
			.Returns((GuidelineMetadata?)null);

		var result = await _controller.DeleteGuideline(id, CancellationToken.None);

		var notFoundResult = Assert.IsType<NotFoundResult>(result);
		Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
	}

	private class FormFileMock : IFormFile
	{
		private readonly string _fileName;
		private readonly byte[] _contentBytes;

		public FormFileMock(string fileName, string content)
		{
			_fileName = fileName;
			_contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
		}

		public string ContentType => "application/json";
		public string ContentDisposition => "form-data";
		public IHeaderDictionary Headers => new HeaderDictionary();
		public long Length => _contentBytes.Length;
		public string Name => _fileName;
		public string FileName => _fileName;

		public Stream OpenReadStream() => new MemoryStream(_contentBytes);
		public void CopyTo(Stream target) => OpenReadStream().CopyTo(target);
		public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
		{
			using (var stream = OpenReadStream())
			{
				await stream.CopyToAsync(target, cancellationToken);
			}
		}
	}

	private static IFormFile CreateMockFormFile(string fileName, string content)
	{
		return new FormFileMock(fileName, content);
	}
}
