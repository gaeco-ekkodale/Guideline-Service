// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using GuidelineService.Api.Models;

namespace GuidelineService.Api.DTOs;

/// <summary>
/// Represents the metadata of an uploaded guideline returned to API clients.
/// </summary>
public class GuidelineDto
{
	/// <summary>The unique identifier of the guideline.</summary>
	public Guid Id
	{
		get; set;
	}

	/// <summary>The display name of the guideline (derived from the file name).</summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>The original uploaded file name including its extension.</summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>The content type (MIME type) of the stored file.</summary>
	public string ContentType { get; set; } = string.Empty;

	/// <summary>The size of the stored file in bytes.</summary>
	public long Size
	{
		get; set;
	}

	/// <summary>The ETag of the stored file.</summary>
	public string Etag { get; set; } = string.Empty;

	/// <summary>The timestamp when the guideline was first uploaded.</summary>
	public DateTimeOffset CreatedAt
	{
		get; set;
	}

	/// <summary>The timestamp when the guideline file was last replaced.</summary>
	public DateTimeOffset UpdatedAt
	{
		get; set;
	}

	/// <summary>
	/// Creates a <see cref="GuidelineDto"/> from a <see cref="GuidelineMetadata"/> domain entity.
	/// </summary>
	/// <param name="guideline">The source guideline metadata.</param>
	/// <returns>A populated <see cref="GuidelineDto"/>.</returns>
	public static GuidelineDto FromEntity(GuidelineMetadata guideline) => new()
	{
		Id = guideline.Id,
		Name = guideline.Name,
		FileName = guideline.FileName,
		ContentType = guideline.ContentType,
		Size = guideline.Size,
		Etag = guideline.Etag,
		CreatedAt = guideline.CreatedAt,
		UpdatedAt = guideline.UpdatedAt
	};
}
