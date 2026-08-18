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
using Microsoft.EntityFrameworkCore;

namespace GuidelineService.Api.Infrastructure;

/// <summary>
/// Represents the database context for the guideline service, providing access to the underlying database
/// and managing the entity models.
/// </summary>
public class GuidelineDbContext : DbContext
{
	/// <summary>
	/// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="GuidelineMetadata"/> entities.
	/// </summary>
	public DbSet<GuidelineMetadata> Guidelines
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="OutboxEvent"/> entities.
	/// </summary>
	public DbSet<OutboxEvent> OutboxEvents
	{
		get; set;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GuidelineDbContext"/> class.
	/// </summary>
	/// <param name="options">The options to be used by the DbContext.</param>
	public GuidelineDbContext(DbContextOptions<GuidelineDbContext> options) : base(options)
	{
	}

	/// <summary>
	/// Configures the model that was discovered by convention from the entity types.
	/// </summary>
	/// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<GuidelineMetadata>().HasKey(x => x.Id);

		modelBuilder.Entity<OutboxEvent>().HasKey(x => x.Id);
		modelBuilder.Entity<OutboxEvent>()
			.Property(p => p.Payload)
			.HasColumnType("text")
			.IsRequired(false);

		base.OnModelCreating(modelBuilder);
	}
}
