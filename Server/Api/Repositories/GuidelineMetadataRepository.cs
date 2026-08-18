// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using GuidelineService.Api.Infrastructure;
using GuidelineService.Api.Models;
using GuidelineService.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GuidelineService.Api.Repositories;

/// <summary>
/// Implements <see cref="IGuidelineMetadataRepository"/> using an Entity Framework Core context.
/// </summary>
public class GuidelineMetadataRepository : IGuidelineMetadataRepository
{
	private readonly GuidelineDbContext _context;

	/// <summary>
	/// Initializes a new instance of the <see cref="GuidelineMetadataRepository"/> class.
	/// </summary>
	/// <param name="context">The database context to be used for data operations.</param>
	public GuidelineMetadataRepository(GuidelineDbContext context)
	{
		_context = context;
	}

	/// <inheritdoc />
	public void Add(GuidelineMetadata guideline)
	{
		if (guideline == null)
			throw new ArgumentNullException(nameof(guideline));
		_context.Guidelines.Add(guideline);
	}

	/// <inheritdoc />
	public async Task<List<GuidelineMetadata>> GetAllAsync(CancellationToken ct = default)
	{
		return await _context.Guidelines
			.OrderByDescending(g => g.CreatedAt)
			.ToListAsync(ct);
	}

	/// <inheritdoc />
	public async Task<GuidelineMetadata?> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		return await _context.Guidelines.FirstOrDefaultAsync(g => g.Id == id, ct);
	}

	/// <inheritdoc />
	public void Remove(GuidelineMetadata guideline)
	{
		if (guideline == null)
			throw new ArgumentNullException(nameof(guideline));
		_context.Guidelines.Remove(guideline);
	}

	/// <inheritdoc />
	public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
