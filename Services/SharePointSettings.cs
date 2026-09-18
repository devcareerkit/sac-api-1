namespace DsacReporting.Api.Services;

public class SharePointSettings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    // e.g. "yourtenant.sharepoint.com" and "sites/DSAC"
    public string SiteHostname { get; set; } = string.Empty;
    public string SitePath { get; set; } = string.Empty;

    // Document library name, defaults to the site's default drive if empty.
    public string DriveName { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(SiteHostname);
}
