using DsacReporting.Api.Services;
using Xunit;

namespace DsacReporting.Api.Tests;

public class IndicatorMatcherTests
{
    [Fact]
    public void FindMatch_ReturnsMatched_ForExactName()
    {
        var id = Guid.NewGuid();
        var candidates = new[] { (id, "Number of capacity development programmes implemented by NAC") };

        var (matchedId, confidence) = IndicatorMatcher.FindMatch(
            "Number of capacity development programmes implemented by NAC", candidates);

        Assert.Equal(id, matchedId);
        Assert.Equal("matched", confidence);
    }

    [Fact]
    public void FindMatch_ReturnsMatched_ForCloseVariant()
    {
        var id = Guid.NewGuid();
        var candidates = new[] { (id, "Job creation") };

        var (matchedId, confidence) = IndicatorMatcher.FindMatch("Job Creation", candidates);

        Assert.Equal(id, matchedId);
        Assert.Equal("matched", confidence);
    }

    [Fact]
    public void FindMatch_ReturnsUnmatched_ForUnrelatedName()
    {
        var candidates = new[] { (Guid.NewGuid(), "Job creation") };

        var (matchedId, confidence) = IndicatorMatcher.FindMatch(
            "Number of arts related publications disseminated", candidates);

        Assert.Null(matchedId);
        Assert.Equal("unmatched", confidence);
    }

    [Fact]
    public void FindMatch_ReturnsUnmatched_WhenNoCandidates()
    {
        var (matchedId, confidence) = IndicatorMatcher.FindMatch("Anything", Array.Empty<(Guid, string)>());

        Assert.Null(matchedId);
        Assert.Equal("unmatched", confidence);
    }
}
