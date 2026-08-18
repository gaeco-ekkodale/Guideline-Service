// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Reflection;
using Ekkodale.TelemetryExtensions;
using GuidelineModelIO;
using GuidelineService.Api.Extensions.ServiceExtensions;
using GuidelineService.Api.Infrastructure;
using GuidelineService.Api.Messaging.Producer;
using GuidelineService.Api.Middleware;
using GuidelineService.Api.Options;
using GuidelineService.Api.Repositories;
using GuidelineService.Api.Repositories.Interfaces;
using GuidelineService.Api.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Minio;
using Throw;

var builder = WebApplication.CreateBuilder(args);

ConfigurationManager configuration = builder.Configuration;

TelemetryOptions? telOpts = configuration.GetSection("OpenTelemetry").Get<TelemetryOptions>();
telOpts.ThrowIfNull("OpenTelemetry configuration is missing");
builder.AddMonitoring(telOpts, Assembly.GetExecutingAssembly());

builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Version = "v1",
		Title = "Guideline API",
		Description = "An ASP.NET Core Web API for managing guideline",
	});
	var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
	options.IncludeXmlComments(System.IO.Path.Combine(AppContext.BaseDirectory, xmlFilename), true);
});

builder.Services.AddOptions<KeycloakOptions>()
	.Bind(builder.Configuration.GetSection("Keycloak"))
	.ValidateDataAnnotations();

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
	// GuidelineReaderWriter.GetSettings() was removed in Guideline.Model 2.1.12.
	// Guideline (de)serialization is handled by GuidelineReaderWriter directly,
	// so the global MVC serializer keeps its default TypeNameHandling.None.
});
builder.Services.AddRouting(options => options.LowercaseUrls = true);

//MapperConfiguration mapperConfiguration = new MapperConfiguration((cfg) =>
//{
//	cfg.AddProfile<MapperProfile>(); //Adding the Mappings from the MapperProfile
//});

//builder.Services.AddSingleton(new Mapper(mapperConfiguration));

GuidelineReaderWriter guidelineReaderWriter = new();
builder.Services.AddSingleton(guidelineReaderWriter);

//builder.Services.AddSingleton(new GuidelineManager(guidelineReaderWriter));

//builder.Services.AddSingleton<ClassificationService>();

builder.Services.Configure<MinioOptions>(configuration.GetSection("Minio"));
builder.Services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
builder.Services.AddOptions<PostgresOptions>()
    .Bind(configuration.GetSection(PostgresOptions.Postgres))
    .ValidateDataAnnotations();

builder.Services.AddPostgres();

var minioOptions = configuration.GetSection("Minio").Get<MinioOptions>();
builder.Services.AddSingleton<IMinioClient>(_ =>
    new MinioClient()
        .WithEndpoint(minioOptions!.Address)
        .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
        .WithSSL(minioOptions.Address.StartsWith("https"))
        .Build());

builder.Services.AddScoped<IGuidelineStorageRepository, GuidelineStorageRepository>();
builder.Services.AddScoped<IGuidelineMetadataRepository, GuidelineMetadataRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddHostedService<OutboxProcessorHostedService>();
builder.Services.AddScoped<GuidelineAppService>();

builder.WebHost.ConfigureKestrel(options =>
{
	options.Limits.MaxRequestBodySize = 1073741824; // 1GB
});

builder.Services.Configure<FormOptions>(options =>
{
	options.MultipartBodyLengthLimit = 1073741824; // 1GB
});

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddCors(options =>
	{
		options.AddPolicy("AllowAllOrigins",
			builder => builder
				.AllowAnyOrigin()  // Allowing any origin
				.AllowAnyMethod()  // Allowing any HTTP method
				.AllowAnyHeader()); // Allowing any header
	});
}
else
{
	var allowedCorsOrigin = builder.Configuration["AllowedCorsOrigins:ServerUrl"];

	builder.Services.AddCors(options =>
	{
		options.AddPolicy("AllowSpecificOrigin",
		builder => builder
			.WithOrigins(allowedCorsOrigin)
			.AllowAnyHeader()
			.AllowAnyMethod());
	});
}

#region Authentication
builder.Services.AddKeycloakAuthentication(options =>
{
	configuration.GetSection("Keycloak").Bind(options);
});

#endregion

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<GuidelineDbContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation("Database Creation ensured.");
    }
    catch (Exception e)
    {
        logger.LogError(e, "Database Creation failed!");
        Console.WriteLine(e.Message);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseCors("AllowSpecificOrigin");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
else
{
	app.UseCors("AllowAllOrigins");
}

// Respect reverse proxy headers (Traefik) for scheme/host
var fwdOptions = new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
};
fwdOptions.KnownNetworks.Clear();
fwdOptions.KnownProxies.Clear();
app.UseForwardedHeaders(fwdOptions);

app.UseSwagger(c =>
{
	c.PreSerializeFilters.Add((swagger, httpReq) =>
	{
		var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
		var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpReq.Host.Value;
		var basePath = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? httpReq.PathBase.Value ?? string.Empty;

		swagger.Servers = [
			new OpenApiServer { Url = $"{scheme}://{host}{basePath}" }
		];
	});
});
app.UseSwaggerUI(options =>
{
	options.SwaggerEndpoint("v1/swagger.json", "v1");
	options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.UseMiddleware<ExceptionHandlingMiddleware>();

await app.RunAsync();
