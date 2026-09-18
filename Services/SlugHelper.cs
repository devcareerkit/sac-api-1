using System.Text.RegularExpressions;

namespace DsacReporting.Api.Services;

public static class SlugHelper
{
    public static string ToSlug(string name)
    {
        var lower = name.ToLowerInvariant();
        var withHyphens = Regex.Replace(lower, @"[^a-z0-9]+", "-");
        return withHyphens.Trim('-');
    }

    public static string ExtractFileName(string fileUrl)
    {
        var lastSegment = fileUrl.TrimEnd('/').Split('/').LastOrDefault() ?? fileUrl;
        return Uri.UnescapeDataString(lastSegment);
    }
}
