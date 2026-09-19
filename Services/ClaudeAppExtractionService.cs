using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;

namespace DsacReporting.Api.Services;

public class ClaudeAppExtractionService : IAppExtractionService
{
    private readonly AnthropicSettings _settings;
    private readonly Lazy<AnthropicClient> _client;

    public ClaudeAppExtractionService(IOptions<AnthropicSettings> settings)
    {
        _settings = settings.Value;
        _client = new Lazy<AnthropicClient>(() => new AnthropicClient { ApiKey = _settings.ApiKey });
    }

    public bool IsConfigured => _settings.IsConfigured;

    public async Task<ExtractionResult> ExtractAsync(byte[] pdfBytes, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Claude is not configured. Set the Anthropic__ApiKey environment variable.");
        }

        var base64 = Convert.ToBase64String(pdfBytes);

        var schema = new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(new
            {
                summary = new { type = "string" },
                indicators = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string" },
                            annual_target = new { type = new[] { "number", "null" } },
                            unit = new { type = new[] { "string", "null" } },
                            quarterly_targets = new
                            {
                                type = "array",
                                items = new { type = new[] { "number", "null" } },
                                minItems = 4,
                                maxItems = 4,
                            },
                        },
                        required = new[] { "name", "annual_target", "unit", "quarterly_targets" },
                        additionalProperties = false,
                    },
                },
            }),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "summary", "indicators" }),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        };

        var parameters = new MessageCreateParams
        {
            Model = "claude-opus-5",
            MaxTokens = 16000,
            OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = schema } },
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = new List<ContentBlockParam>
                    {
                        new DocumentBlockParam { Source = new Base64PdfSource { Data = base64 } },
                        new TextBlockParam
                        {
                            Text = "This is a government Annual Performance Plan (APP). Read Part C " +
                                   "(\"Measuring Our Performance\" / \"Outcomes, Outputs, Performance " +
                                   "Indicators and Targets\") and Part D (\"Technical Indicator " +
                                   "Descriptions\") if present. Extract every Output Indicator you find, " +
                                   "with its annual target for the current/nearest year, its unit, and its " +
                                   "quarterly target split (Q1-Q4; use null for any quarter you cannot " +
                                   "determine). Also write a 2-4 sentence plain-language summary of the " +
                                   "APP's overall strategic focus.",
                        },
                    },
                },
            ],
        };

        var response = await _client.Value.Messages.Create(parameters, cancellationToken: ct);

        if (response.StopReason != StopReason.EndTurn && response.StopReason != StopReason.MaxTokens)
        {
            throw new InvalidOperationException($"Claude did not complete normally (stop_reason={response.StopReason}).");
        }

        var text = response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .Select(b => b.Text)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Claude returned no text content.");

        return ParseResponse(text);
    }

    public static ExtractionResult ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var summary = root.GetProperty("summary").GetString() ?? string.Empty;
        var indicators = new List<ExtractedIndicator>();

        foreach (var item in root.GetProperty("indicators").EnumerateArray())
        {
            var name = item.GetProperty("name").GetString() ?? string.Empty;
            var annualTarget = item.TryGetProperty("annual_target", out var at) && at.ValueKind == JsonValueKind.Number
                ? at.GetDecimal() : (decimal?)null;
            var unit = item.TryGetProperty("unit", out var u) && u.ValueKind == JsonValueKind.String
                ? u.GetString() : null;

            var quarterlyTargets = new decimal?[4];
            if (item.TryGetProperty("quarterly_targets", out var qt) && qt.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var q in qt.EnumerateArray())
                {
                    if (i >= 4) break;
                    quarterlyTargets[i] = q.ValueKind == JsonValueKind.Number ? q.GetDecimal() : (decimal?)null;
                    i++;
                }
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                indicators.Add(new ExtractedIndicator(name, annualTarget, unit, quarterlyTargets));
            }
        }

        return new ExtractionResult(summary, indicators);
    }
}
