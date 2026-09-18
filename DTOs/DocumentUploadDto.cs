namespace DsacReporting.Api.DTOs;

public class DocumentUploadResponseDto
{
    public Guid Id { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public int Version { get; set; }
}
