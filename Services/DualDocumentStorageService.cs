using Microsoft.Extensions.Logging;

namespace DsacReporting.Api.Services;

// Writes documents to Postgres (primary, authoritative for FileUrl/downloads) and
// best-effort mirrors the same bytes to SharePoint (secondary). A SharePoint failure
// (e.g. missing write permission) is logged but never fails the upload - SharePoint
// access can be fixed later without losing any documents, since the DB copy is always
// there.
public class DualDocumentStorageService : IDocumentStorageService
{
    private readonly DbDocumentStorageService _primary;
    private readonly SharePointDocumentStorageService _secondary;
    private readonly ILogger<DualDocumentStorageService> _logger;

    public DualDocumentStorageService(
        DbDocumentStorageService primary,
        SharePointDocumentStorageService secondary,
        ILogger<DualDocumentStorageService> logger)
    {
        _primary = primary;
        _secondary = secondary;
        _logger = logger;
    }

    public bool IsConfigured => _primary.IsConfigured;

    public async Task<UploadedFile> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, ct);
        memory.Position = 0;

        var uploaded = await _primary.UploadAsync(memory, fileName, contentType, ct);

        if (_secondary.IsConfigured)
        {
            try
            {
                memory.Position = 0;
                await _secondary.UploadAsync(memory, fileName, contentType, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SharePoint mirror upload failed for '{FileName}'; document is still saved in the database.", fileName);
            }
        }

        return uploaded;
    }

    public Task<byte[]> DownloadAsync(string fileUrl, CancellationToken ct = default) =>
        _primary.DownloadAsync(fileUrl, ct);
}
