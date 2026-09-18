namespace DsacReporting.Api.Services;

public static class IndicatorMatcher
{
    // Normalized Levenshtein similarity, 0..1. A simple, dependency-free
    // approach appropriate for matching a handful of KPI names per entity —
    // no embedding call needed at this volume.
    private const double MatchThreshold = 0.55;

    public static (Guid? EntityKpiId, string Confidence) FindMatch(
        string extractedName, IEnumerable<(Guid Id, string KpiName)> candidates)
    {
        var normalizedExtracted = Normalize(extractedName);
        (Guid Id, string KpiName)? best = null;
        var bestScore = 0.0;

        foreach (var candidate in candidates)
        {
            var score = Similarity(normalizedExtracted, Normalize(candidate.KpiName));
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best is not null && bestScore >= MatchThreshold)
        {
            return (best.Value.Id, "matched");
        }

        return (null, "unmatched");
    }

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant();

    private static double Similarity(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0.0;

        var distance = LevenshteinDistance(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var costs = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) costs[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            costs[0] = i;
            var previousDiagonal = i - 1;
            for (var j = 1; j <= b.Length; j++)
            {
                var previousDiagonalSave = costs[j];
                costs[j] = a[i - 1] == b[j - 1]
                    ? previousDiagonal
                    : 1 + Math.Min(previousDiagonal, Math.Min(costs[j], costs[j - 1]));
                previousDiagonal = previousDiagonalSave;
            }
        }

        return costs[b.Length];
    }
}
