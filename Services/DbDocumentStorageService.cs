using DsacReporting.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

// Interim document storage: files land in Postgres instead of SharePoint.
// Always configured (no credentials needed) — drop-in replacement for
// SharePointDocumentStorageService via DI registration in Program.cs.
// fileUrl is a synthetic "db://<id>/<url-encoded-filename>" reference, not a
// real browser-navigable URL.
public class DbDocumentStorageService : IDocumentStorageService
{
    private readonly AppDbContext _db;
    public DbDocumentStorageService(AppDbContext db) => _db = db;

    public bool IsConfigured => true;

    public async Task<UploadedFile> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, ct);

        var blob = new Data.Entities.DocumentBlob
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Content = memory.ToArray(),
            UploadedAt = DateTimeOffset.UtcNow,
        };

        _db.DocumentBlobs.Add(blob);
        await _db.SaveChangesAsync(ct);

        var fileUrl = $"db://{blob.Id}/{Uri.EscapeDataString(fileName)}";
        return new UploadedFile(fileUrl, 1);
    }

    public async Task<byte[]> DownloadAsync(string fileUrl, CancellationToken ct = default)
    {
        var id = ParseId(fileUrl);
        var blob = await _db.DocumentBlobs.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new InvalidOperationException($"No stored document found for '{fileUrl}'.");

        return blob.Content;
    }

    private static Guid ParseId(string fileUrl)
    {
        // db://<id>/<filename>
        var withoutScheme = fileUrl.StartsWith("db://", StringComparison.Ordinal)
            ? fileUrl["db://".Length..]
            : fileUrl;

        var idSegment = withoutScheme.Split('/').FirstOrDefault()
            ?? throw new InvalidOperationException($"Malformed document reference '{fileUrl}'.");

        if (!Guid.TryParse(idSegment, out var id))
        {
            throw new InvalidOperationException($"Malformed document reference '{fileUrl}'.");
        }

        return id;
    }
}
