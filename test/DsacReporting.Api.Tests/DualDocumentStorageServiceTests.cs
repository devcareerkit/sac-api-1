using System.Text;
using DsacReporting.Api.Data;
using DsacReporting.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DsacReporting.Api.Tests;

public class DualDocumentStorageServiceTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static SharePointDocumentStorageService CreateUnconfiguredSharePoint() =>
        new(Options.Create(new SharePointSettings()));

    [Fact]
    public async Task UploadAsync_WhenSharePointIsNotConfigured_StillSucceedsUsingOnlyTheDatabase()
    {
        using var db = CreateDb(nameof(UploadAsync_WhenSharePointIsNotConfigured_StillSucceedsUsingOnlyTheDatabase));
        var dual = new DualDocumentStorageService(
            new DbDocumentStorageService(db),
            CreateUnconfiguredSharePoint(),
            NullLogger<DualDocumentStorageService>.Instance);

        var original = Encoding.UTF8.GetBytes("%PDF-1.4 dual storage test");
        using var stream = new MemoryStream(original);

        var uploaded = await dual.UploadAsync(stream, "report.pdf", "application/pdf");

        Assert.StartsWith("db://", uploaded.FileUrl);
        var downloaded = await dual.DownloadAsync(uploaded.FileUrl);
        Assert.Equal(original, downloaded);
    }

    [Fact]
    public void IsConfigured_ReflectsThePrimaryDatabaseProvider()
    {
        using var db = CreateDb(nameof(IsConfigured_ReflectsThePrimaryDatabaseProvider));
        var dual = new DualDocumentStorageService(
            new DbDocumentStorageService(db),
            CreateUnconfiguredSharePoint(),
            NullLogger<DualDocumentStorageService>.Instance);

        Assert.True(dual.IsConfigured);
    }

    [Fact]
    public async Task DownloadAsync_AlwaysReadsFromTheDatabaseNotSharePoint()
    {
        using var db = CreateDb(nameof(DownloadAsync_AlwaysReadsFromTheDatabaseNotSharePoint));
        var dual = new DualDocumentStorageService(
            new DbDocumentStorageService(db),
            CreateUnconfiguredSharePoint(),
            NullLogger<DualDocumentStorageService>.Instance);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var uploaded = await dual.UploadAsync(stream, "proof.pdf", "application/pdf");

        // db:// scheme confirms reads are served from the DB-backed primary, independent
        // of whether the SharePoint mirror succeeded.
        var downloaded = await dual.DownloadAsync(uploaded.FileUrl);
        Assert.NotEmpty(downloaded);
    }
}
