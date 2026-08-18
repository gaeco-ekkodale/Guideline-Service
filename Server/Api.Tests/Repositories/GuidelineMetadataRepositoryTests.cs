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
using GuidelineService.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GuidelineService.Api.Tests.Repositories;

public class GuidelineMetadataRepositoryTests : IDisposable
{
    private readonly GuidelineDbContext _context;
    private readonly GuidelineMetadataRepository _repo;

    public GuidelineMetadataRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<GuidelineDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new GuidelineDbContext(options);
        _repo = new GuidelineMetadataRepository(_context);
    }

    [Fact]
    public void Add_WithValidGuideline_StagesForInsertion()
    {
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(guideline);

        Assert.Contains(guideline, _context.Guidelines.Local);
    }

    [Fact]
    public void Add_WithNullGuideline_ThrowsArgumentNullException()
    {
        var action = () => _repo.Add(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleGuidelines_ReturnsOrderedByCreatedAtDescending()
    {
        var now = DateTimeOffset.UtcNow;
        var guideline1 = new GuidelineMetadata(Guid.NewGuid(), "name1", "name1.json", "application/json",
            "guidelines", "id1.json", "etag1", 100, now.AddHours(-2));
        var guideline2 = new GuidelineMetadata(Guid.NewGuid(), "name2", "name2.json", "application/json",
            "guidelines", "id2.json", "etag2", 100, now.AddHours(-1));
        var guideline3 = new GuidelineMetadata(Guid.NewGuid(), "name3", "name3.json", "application/json",
            "guidelines", "id3.json", "etag3", 100, now);

        _repo.Add(guideline1);
        _repo.Add(guideline2);
        _repo.Add(guideline3);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(guideline3.Id, result[0].Id);
        Assert.Equal(guideline2.Id, result[1].Id);
        Assert.Equal(guideline1.Id, result[2].Id);
    }

    [Fact]
    public async Task GetAllAsync_WithNoGuidelines_ReturnsEmptyList()
    {
        var result = await _repo.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsGuideline()
    {
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(guideline);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("name", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void Remove_WithValidGuideline_StagesForDeletion()
    {
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(guideline);
        _repo.Remove(guideline);

        Assert.DoesNotContain(guideline, _context.Guidelines.Local);
    }

    [Fact]
    public void Remove_WithNullGuideline_ThrowsArgumentNullException()
    {
        var action = () => _repo.Remove(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task SaveChangesAsync_WithAddedGuideline_PersistsToDatabase()
    {
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(guideline);
        var changes = await _repo.SaveChangesAsync();

        Assert.Equal(1, changes);

        var result = await _repo.GetByIdAsync(id);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithRemovedGuideline_DeletesFromDatabase()
    {
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(guideline);
        await _repo.SaveChangesAsync();

        _repo.Remove(guideline);
        var changes = await _repo.SaveChangesAsync();

        Assert.Equal(1, changes);

        var result = await _repo.GetByIdAsync(id);
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
    {
        var changes = await _repo.SaveChangesAsync();

        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task SaveChangesAsync_RespectsCancellationToken()
    {
        var id = Guid.NewGuid();
        var guideline = new GuidelineMetadata(id, "name", "name.json", "application/json",
            "guidelines", "id.json", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(guideline);
        var ct = CancellationToken.None;

        var changes = await _repo.SaveChangesAsync(ct);

        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task ReplaceFile_UpdatesFileRelatedFields()
    {
        var id = Guid.NewGuid();
        var originalGuideline = new GuidelineMetadata(id, "original", "original.json", "application/json",
            "guidelines", "original.json", "etag1", 100, DateTimeOffset.UtcNow);

        _repo.Add(originalGuideline);
        await _repo.SaveChangesAsync();

        var retrieved = await _repo.GetByIdAsync(id);
        var newTime = DateTimeOffset.UtcNow.AddMinutes(1);
        retrieved!.ReplaceFile("newname", "newname.json", "newname.json", "etag2", 200, newTime);

        await _repo.SaveChangesAsync();

        var updated = await _repo.GetByIdAsync(id);
        Assert.Equal("newname", updated!.Name);
        Assert.Equal("newname.json", updated.FileName);
        Assert.Equal("newname.json", updated.ObjectKey);
        Assert.Equal("etag2", updated.Etag);
        Assert.Equal(200, updated.Size);
        Assert.Equal(newTime, updated.UpdatedAt);
        Assert.True(updated.CreatedAt < updated.UpdatedAt);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
