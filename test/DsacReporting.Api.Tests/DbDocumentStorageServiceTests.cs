using System.Text;
using DsacReporting.Api.Data;
using DsacReporting.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DsacReporting.Api.Tests;

public class DbDocumentStorageServiceTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void IsConfigured_IsAlwaysTrue()
    {
        using var db = CreateDb(nameof(IsConfigured_IsAlwaysTrue));
        var service = new DbDocumentStorageService(db);

        Assert.True(service.IsConfigured);
    }

    [Fact]
    public async Task UploadAsync_ThenDownloadAsync_RoundTripsTheOriginalBytes()
    {
        using var db = CreateDb(nameof(UploadAsync_ThenDownloadAsync_RoundTripsTheOriginalBytes));
        var service = new DbDocumentStorageService(db);

        var original = Encoding.UTF8.GetBytes("%PDF-1.4 fake content for round-trip test");
        using var stream = new MemoryStream(original);

        var uploaded = await service.UploadAsync(stream, "Q3_Financials.pdf", "application/pdf");

        Assert.StartsWith("db://", uploaded.FileUrl);
        Assert.Contains("Q3_Financials.pdf", uploaded.FileUrl);

        var downloaded = await service.DownloadAsync(uploaded.FileUrl);

        Assert.Equal(original, downloaded);
    }

    [Fact]
    public async Task ExtractFileName_RecoversTheOriginalFileNameFromTheStoredUrl()
    {
        using var db = CreateDb(nameof(ExtractFileName_RecoversTheOriginalFileNameFromTheStoredUrl));
        var service = new DbDocumentStorageService(db);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var uploaded = await service.UploadAsync(stream, "Annual Report 2026.pdf", "application/pdf");

        var fileName = SlugHelper.ExtractFileName(uploaded.FileUrl);

        Assert.Equal("Annual Report 2026.pdf", fileName);
    }

    [Fact]
    public async Task DownloadAsync_ThrowsForAnUnknownReference()
    {
        using var db = CreateDb(nameof(DownloadAsync_ThrowsForAnUnknownReference));
        var service = new DbDocumentStorageService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DownloadAsync($"db://{Guid.NewGuid()}/missing.pdf"));
    }
}
