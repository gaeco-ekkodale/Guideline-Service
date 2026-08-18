// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using GuidelineService.Api.DTOs;
using GuidelineService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace GuidelineService.Api.Controllers;

/// <summary>
/// API controller for managing multiple guidelines. Each guideline file is stored in object storage
/// while its metadata is persisted relationally; every change emits a Kafka event via the outbox.
/// </summary>
[Route("[controller]")]
[ApiController]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Authorize]
public class GuidelineController : ControllerBase
{
	private readonly GuidelineAppService _guidelineService;
	private readonly ILogger<GuidelineController> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="GuidelineController"/> class.
	/// </summary>
	/// <param name="guidelineService">The application service orchestrating guideline operations.</param>
	/// <param name="logger">The logger.</param>
	public GuidelineController(GuidelineAppService guidelineService, ILogger<GuidelineController> logger)
	{
		_guidelineService = guidelineService;
		_logger = logger;
	}

	/// <summary>
	/// Uploads a new guideline (JSON). A new identifier is generated server-side.
	/// </summary>
	/// <param name="file">The guideline JSON file to upload.</param>
	/// <returns>The metadata of the created guideline.</returns>
	/// <response code="201">The guideline was created.</response>
	/// <response code="400">The file is missing or invalid.</response>
	[SwaggerOperation(
		Summary = "Uploads a new guideline",
		Description = "Uploads a guideline JSON file, validates and stores it, and creates its metadata. A new id is generated.",
		OperationId = "UploadGuideline",
		Tags = new[] { "guideline" }
	)]
	[ProducesResponseType(typeof(GuidelineDto), StatusCodes.Status201Created)]
	[HttpPost]
	public async Task<IActionResult> UploadGuideline(IFormFile file, CancellationToken ct)
	{
		var result = await _guidelineService.CreateAsync(file, ct);
		if (!result.Success)
			return BadRequest(result.Error);

		var dto = GuidelineDto.FromEntity(result.Guideline!);
		return CreatedAtAction(nameof(GetGuideline), new
		{
			id = dto.Id
		}, dto);
	}

	/// <summary>
	/// Returns the metadata of all uploaded guidelines.
	/// </summary>
	/// <returns>A list of guideline metadata entries.</returns>
	/// <response code="200">Success.</response>
	[SwaggerOperation(
		Summary = "Lists all guidelines",
		Description = "Returns metadata for all uploaded guidelines.",
		OperationId = "GetGuidelines",
		Tags = new[] { "guideline" }
	)]
	[ProducesResponseType(typeof(IEnumerable<GuidelineDto>), StatusCodes.Status200OK)]
	[HttpGet("/guidelines")]
	public async Task<IActionResult> GetGuidelines(CancellationToken ct)
	{
		var guidelines = await _guidelineService.GetAllAsync(ct);
		return Ok(guidelines.Select(GuidelineDto.FromEntity));
	}

	/// <summary>
	/// Returns the metadata of a single guideline.
	/// </summary>
	/// <param name="id">The identifier of the guideline.</param>
	/// <returns>The guideline metadata.</returns>
	/// <response code="200">Success.</response>
	/// <response code="404">No guideline exists with the given id.</response>
	[SwaggerOperation(
		Summary = "Gets a single guideline",
		Description = "Returns the metadata of the guideline with the given id.",
		OperationId = "GetGuideline",
		Tags = new[] { "guideline" }
	)]
	[ProducesResponseType(typeof(GuidelineDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetGuideline(Guid id, CancellationToken ct)
	{
		var guideline = await _guidelineService.GetByIdAsync(id, ct);
		if (guideline is null)
			return NotFound();

		return Ok(GuidelineDto.FromEntity(guideline));
	}

	/// <summary>
	/// Downloads the raw guideline file.
	/// </summary>
	/// <param name="id">The identifier of the guideline.</param>
	/// <returns>The guideline file content.</returns>
	/// <response code="200">Success.</response>
	/// <response code="404">No guideline exists with the given id.</response>
	[SwaggerOperation(
		Summary = "Downloads the guideline file",
		Description = "Returns the raw stored guideline JSON file.",
		OperationId = "GetGuidelineFile",
		Tags = new[] { "guideline" }
	)]
	[ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[HttpGet("{id:guid}/file")]
	public async Task<IActionResult> GetGuidelineFile(Guid id, CancellationToken ct)
	{
		var fileResult = await _guidelineService.GetFileAsync(id, ct);
		if (fileResult is null)
			return NotFound();

		return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
	}

	/// <summary>
	/// Replaces the file of an existing guideline. The guideline id is preserved.
	/// </summary>
	/// <param name="id">The identifier of the guideline to update.</param>
	/// <param name="file">The new guideline file.</param>
	/// <returns>The updated guideline metadata.</returns>
	/// <response code="200">The guideline file was replaced.</response>
	/// <response code="400">The file is missing or invalid.</response>
	/// <response code="404">No guideline exists with the given id.</response>
	[SwaggerOperation(
		Summary = "Replaces a guideline file",
		Description = "Overwrites the file of an existing guideline and notifies downstream services.",
		OperationId = "UpdateGuideline",
		Tags = new[] { "guideline" }
	)]
	[ProducesResponseType(typeof(GuidelineDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[HttpPut("{id:guid}")]
	public async Task<IActionResult> UpdateGuideline(Guid id, IFormFile file, CancellationToken ct)
	{
		var result = await _guidelineService.ReplaceFileAsync(id, file, ct);
		if (result.NotFound)
			return NotFound();
		if (!result.Success)
			return BadRequest(result.Error);

		return Ok(GuidelineDto.FromEntity(result.Guideline!));
	}

	/// <summary>
	/// Deletes a guideline, its file, and notifies downstream services.
	/// </summary>
	/// <param name="id">The identifier of the guideline to delete.</param>
	/// <response code="204">The guideline was deleted.</response>
	/// <response code="404">No guideline exists with the given id.</response>
	[SwaggerOperation(
		Summary = "Deletes a guideline",
		Description = "Deletes the guideline metadata and file and notifies downstream services.",
		OperationId = "DeleteGuideline",
		Tags = new[] { "guideline" }
	)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> DeleteGuideline(Guid id, CancellationToken ct)
	{
		var deleted = await _guidelineService.DeleteAsync(id, ct);
		if (!deleted)
			return NotFound();

		return NoContent();
	}
}
