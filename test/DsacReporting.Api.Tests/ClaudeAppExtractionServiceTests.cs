using DsacReporting.Api.Services;
using Xunit;

namespace DsacReporting.Api.Tests;

public class ClaudeAppExtractionServiceTests
{
    [Fact]
    public void ParseResponse_ParsesSummaryAndIndicators()
    {
        const string json = """
        {
          "summary": "A short summary of the APP.",
          "indicators": [
            {
              "name": "Number of workshops implemented",
              "annual_target": 8,
              "unit": "workshops",
              "quarterly_targets": [2, 2, 2, 2]
            },
            {
              "name": "Positive audit opinion",
              "annual_target": null,
              "unit": null,
              "quarterly_targets": [null, null, null, null]
            }
          ]
        }
        """;

        var result = ClaudeAppExtractionService.ParseResponse(json);

        Assert.Equal("A short summary of the APP.", result.Summary);
        Assert.Equal(2, result.Indicators.Count);

        var first = result.Indicators[0];
        Assert.Equal("Number of workshops implemented", first.Name);
        Assert.Equal(8, first.AnnualTarget);
        Assert.Equal("workshops", first.Unit);
        Assert.Equal(new decimal?[] { 2, 2, 2, 2 }, first.QuarterlyTargets);

        var second = result.Indicators[1];
        Assert.Null(second.AnnualTarget);
        Assert.Null(second.Unit);
        Assert.All(second.QuarterlyTargets, Assert.Null);
    }

    [Fact]
    public void ParseResponse_SkipsIndicatorsWithNoName()
    {
        const string json = """
        {
          "summary": "Summary.",
          "indicators": [
            { "name": "", "annual_target": null, "unit": null, "quarterly_targets": [null, null, null, null] }
          ]
        }
        """;

        var result = ClaudeAppExtractionService.ParseResponse(json);

        Assert.Empty(result.Indicators);
    }
}
