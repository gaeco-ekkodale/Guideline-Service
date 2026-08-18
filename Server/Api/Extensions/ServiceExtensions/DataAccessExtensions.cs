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
using GuidelineService.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GuidelineService.Api.Extensions.ServiceExtensions;

/// <summary>
/// Extensions for data access related operations such as registering the Entity Framework Core context.
/// </summary>
public static class DataAccessExtensions
{
	/// <summary>
	/// Registers the <see cref="GuidelineDbContext"/> with a PostgreSQL connection built from
	/// the bound <see cref="PostgresOptions"/>.
	/// </summary>
	/// <param name="services">The service collection.</param>
	public static void AddPostgres(this IServiceCollection services)
	{
		services.AddDbContext<GuidelineDbContext>((provider, builder) =>
		{
			var postgresOptions = provider.GetRequiredService<IOptions<PostgresOptions>>().Value;

			builder.UseNpgsql(
				$"Host={postgresOptions.Host};" +
				$"Port={postgresOptions.Port};" +
				$"Database={postgresOptions.Database};" +
				$"Username={postgresOptions.User};" +
				$"Password={postgresOptions.Password}");
		}, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
	}
}
