using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;

namespace DsacReporting.Api.Services;

public class SharePointDocumentStorageService : IDocumentStorageService
{
    private readonly SharePointSettings _settings;
    private readonly Lazy<GraphServiceClient> _graphClient;
    private string? _driveId;

    public SharePointDocumentStorageService(IOptions<SharePointSettings> settings)
    {
        _settings = settings.Value;
        _graphClient = new Lazy<GraphServiceClient>(CreateClient);
    }

    public bool IsConfigured => _settings.IsConfigured;

    private GraphServiceClient CreateClient()
    {
        var credential = new ClientSecretCredential(_settings.TenantId, _settings.ClientId, _settings.ClientSecret);
        return new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
    }

    private async Task<string> GetDriveIdAsync(CancellationToken ct)
    {
        if (_driveId is not null)
        {
            return _driveId;
        }

        var site = await _graphClient.Value
            .Sites[$"{_settings.SiteHostname}:/{_settings.SitePath.Trim('/')}"]
            .GetAsync(cancellationToken: ct);

        if (site?.Id is null)
        {
            throw new InvalidOperationException("Could not resolve SharePoint site. Check SiteHostname/SitePath configuration.");
        }

        if (string.IsNullOrWhiteSpace(_settings.DriveName))
        {
            var defaultDrive = await _graphClient.Value.Sites[site.Id].Drive.GetAsync(cancellationToken: ct);
            _driveId = defaultDrive?.Id
                ?? throw new InvalidOperationException("Site has no default drive.");
        }
        else
        {
            var drives = await _graphClient.Value.Sites[site.Id].Drives.GetAsync(cancellationToken: ct);
            var match = drives?.Value?.FirstOrDefault(d => d.Name == _settings.DriveName)
                ?? throw new InvalidOperationException($"No drive named '{_settings.DriveName}' found on the site.");
            _driveId = match.Id
                ?? throw new InvalidOperationException($"Drive '{_settings.DriveName}' has no id.");
        }

        return _driveId;
    }

    public async Task<UploadedFile> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "SharePoint is not configured. Set SharePoint__TenantId, SharePoint__ClientId, " +
                "SharePoint__ClientSecret and SharePoint__SiteHostname environment variables.");
        }

        var driveId = await GetDriveIdAsync(ct);

        // Small file upload (< 4MB). Larger files would need an upload session, not needed for the demo.
        var safeName = Uri.EscapeDataString(fileName);
        var driveItem = await _graphClient.Value
            .Drives[driveId]
            .Root
            .ItemWithPath($"DsacReporting/{safeName}")
            .Content
            .PutAsync(content, cancellationToken: ct);

        if (driveItem?.WebUrl is null)
        {
            throw new InvalidOperationException("SharePoint upload did not return a web URL.");
        }

        return new UploadedFile(driveItem.WebUrl, 1);
    }

    public async Task<byte[]> DownloadAsync(string fileUrl, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("SharePoint is not configured.");
        }

        var driveId = await GetDriveIdAsync(ct);
        var fileName = Uri.UnescapeDataString(fileUrl.TrimEnd('/').Split('/').LastOrDefault() ?? fileUrl);

        await using var stream = await _graphClient.Value
            .Drives[driveId]
            .Root
            .ItemWithPath($"DsacReporting/{Uri.EscapeDataString(fileName)}")
            .Content
            .GetAsync(cancellationToken: ct);

        if (stream is null)
        {
            throw new InvalidOperationException($"Could not download file '{fileName}' from SharePoint.");
        }

        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);
        return memory.ToArray();
    }
}
