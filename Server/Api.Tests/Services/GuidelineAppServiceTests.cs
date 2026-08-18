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
using GuidelineService.Api.Services;
using GuidelineService.Api.Validation;
using GuidelineService.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Minio.DataModel.Response;
using System.Net;
using Xunit;

namespace GuidelineService.Api.Tests.Services;

public class GuidelineAppServiceTests
{
    private IGuidelineStorageRepository _storage = null!;
    private IGuidelineMetadataRepository _metadata = null!;
    private IOutboxRepository _outbox = null!;
    private GuidelineReaderWriter _readerWriter = null!;
    private ILogger<GuidelineAppService> _logger = null!;
    private IOptions<MinioOptions> _minioOptions = null!;
    private IOptions<KafkaOptions> _kafkaOptions = null!;
    private GuidelineAppService _service = null!;

    private void SetupDependencies()
    {
        _storage = Substitute.For<IGuidelineStorageRepository>();
        _metadata = Substitute.For<IGuidelineMetadataRepository>();
        _outbox = Substitute.For<IOutboxRepository>();
        _readerWriter = Substitute.For<GuidelineReaderWriter>();
        _logger = Substitute.For<ILogger<GuidelineAppService>>();

        _minioOptions = Microsoft.Extensions.Options.Options.Create(new MinioOptions { BucketName = "guidelines" });
        _kafkaOptions = Microsoft.Extensions.Options.Options.Create(new KafkaOptions { UploadedGuidelineTopic = "guideline-topic" });

        _service = new GuidelineAppService(
            _storage,
            _metadata,
            _outbox,
            _readerWriter,
            _minioOptions,
            _kafkaOptions,
            _logger);
    }

    [Fact]
    public async Task CreateAsync_WithNullFile_ReturnsValidationError()
    {
        SetupDependencies();
        var result = await _service.CreateAsync(null!);

        Assert.False(result.Success);
        Assert.Contains("null or empty", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyFile_ReturnsValidationError()
    {
        SetupDependencies();
        var mockFile = CreateMockFormFile("test.json", "");

        var result = await _service.CreateAsync(mockFile);

        Assert.False(result.Success);
        Assert.Contains("null or empty", result.Error);
    }

    [Fact]
    public async Task ReplaceFileAsync_WithNonExistentId_ReturnsMissingGuideline()
    {
        SetupDependencies();
        var id = Guid.NewGuid();
        var mockFile = CreateMockFormFile("new.json", "{}");

        _metadata.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GuidelineMetadata?)null);

        var result = await _service.ReplaceFileAsync(id, mockFile);

        Assert.False(result.Success);
        Assert.True(result.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingGuideline_RemovesMetadataAndEmitsEvent()
    {
        SetupDependencies();
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        _metadata.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(guideline);

        _metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _service.DeleteAsync(id);

        Assert.True(result);

        _metadata.Received(1).Remove(guideline);
        _outbox.Received(1).Add(Arg.Any<DeletedGuideline>(), "guideline-topic", id.ToString());
        await _storage.Received(1).RemoveAsync("guidelines", "id.json");
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        SetupDependencies();
        var id = Guid.NewGuid();

        _metadata.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GuidelineMetadata?)null);

        var result = await _service.DeleteAsync(id);

        Assert.False(result);

        _metadata.DidNotReceive().Remove(Arg.Any<GuidelineMetadata>());
    }

    [Fact]
    public async Task DeleteAsync_WhenStorageRemovalFails_LogsErrorButSucceeds()
    {
        SetupDependencies();
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        _metadata.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(guideline);

        _metadata.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        _storage.RemoveAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new Exception("Storage error")));

        var result = await _service.DeleteAsync(id);

        Assert.True(result);
        _logger.Received().Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<object>(), Arg.Any<Exception>(), Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingGuideline_ReturnsGuideline()
    {
        SetupDependencies();
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        _metadata.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(guideline);

        var result = await _service.GetByIdAsync(id);

        Assert.Equal(guideline, result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllGuidelines()
    {
        SetupDependencies();
        var guidelines = new List<GuidelineMetadata>
        {
            new(Guid.NewGuid(), "name1", "name1.json", "application/json", "guidelines", "id1.json", "etag1", 100, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "name2", "name2.json", "application/json", "guidelines", "id2.json", "etag2", 200, DateTimeOffset.UtcNow)
        };

        _metadata.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(guidelines);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(guidelines, result);
    }

    [Fact]
    public async Task GetFileAsync_WithExistingGuideline_ReturnsFileContent()
    {
        SetupDependencies();
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        var fileContent = "{}";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

        _metadata.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(guideline);

        _storage.GetAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(fileStream);

        var result = await _service.GetFileAsync(id);

        Assert.NotNull(result);
        Assert.Equal("name.json", result!.FileName);
        Assert.Equal("application/json", result.ContentType);
    }

    [Fact]
    public async Task GetFileAsync_WithNonExistentGuideline_ReturnsNull()
    {
        SetupDependencies();
        var id = Guid.NewGuid();

        _metadata.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GuidelineMetadata?)null);

        var result = await _service.GetFileAsync(id);

        Assert.Null(result);
    }

    private static IFormFile CreateMockFormFile(string fileName, string content)
    {
        var mock = Substitute.For<IFormFile>();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);

        mock.FileName.Returns(fileName);
        mock.Length.Returns(bytes.Length);
        mock.OpenReadStream().Returns(new MemoryStream(bytes));
        mock.ContentType.Returns("application/json");

        return mock;
    }
}
