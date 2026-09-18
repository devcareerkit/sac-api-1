namespace DsacReporting.Api.Data.Entities;

public class DocumentBlob
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public DateTimeOffset UploadedAt { get; set; }
}
