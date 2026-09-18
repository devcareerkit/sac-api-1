namespace DsacReporting.Api.Services;

public record UploadedFile(string FileUrl, int Version);

public interface IDocumentStorageService
{
    bool IsConfigured { get; }

    Task<UploadedFile> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
}
