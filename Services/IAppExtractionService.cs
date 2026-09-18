namespace DsacReporting.Api.Services;

public record ExtractedIndicator(string Name, decimal? AnnualTarget, string? Unit, decimal?[] QuarterlyTargets);

public record ExtractionResult(string Summary, List<ExtractedIndicator> Indicators);

public interface IAppExtractionService
{
    bool IsConfigured { get; }

    Task<ExtractionResult> ExtractAsync(byte[] pdfBytes, CancellationToken ct = default);
}
