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

namespace GuidelineService.Api.Repositories.Interfaces;

/// <summary>
/// Represents a repository for managing guideline metadata persisted in Postgres.
/// </summary>
/// <remarks>
/// Changes are staged on the underlying unit of work and only persisted when
/// <see cref="SaveChangesAsync"/> is called, allowing metadata changes to be committed
/// atomically together with outbox events.
/// </remarks>
public interface IGuidelineMetadataRepository
{
	/// <summary>
	/// Stages a new guideline metadata entry for insertion.
	/// </summary>
	/// <param name="guideline">The guideline metadata to add.</param>
	void Add(GuidelineMetadata guideline);

	/// <summary>
	/// Retrieves all guideline metadata entries, ordered by creation date (newest first).
	/// </summary>
	/// <param name="ct">A cancellation token to cancel the operation.</param>
	Task<List<GuidelineMetadata>> GetAllAsync(CancellationToken ct = default);

	/// <summary>
	/// Retrieves a single guideline metadata entry by its identifier.
	/// </summary>
	/// <param name="id">The unique identifier of the guideline.</param>
	/// <param name="ct">A cancellation token to cancel the operation.</param>
	/// <returns>The guideline metadata, or <c>null</c> if it does not exist.</returns>
	Task<GuidelineMetadata?> GetByIdAsync(Guid id, CancellationToken ct = default);

	/// <summary>
	/// Stages a guideline metadata entry for deletion.
	/// </summary>
	/// <param name="guideline">The guideline metadata to remove.</param>
	void Remove(GuidelineMetadata guideline);

	/// <summary>
	/// Persists all staged changes to the database.
	/// </summary>
	/// <param name="ct">A cancellation token to cancel the operation.</param>
	/// <returns>The number of state entries written to the database.</returns>
	Task<int> SaveChangesAsync(CancellationToken ct = default);
}
