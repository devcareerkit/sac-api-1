namespace DsacReporting.Api.Services;

public class SharePointSettings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    // e.g. "yourtenant.sharepoint.com" and "sites/DSAC"
    // Only needed when DriveId is not set (service resolves the drive via site lookup).
    public string SiteHostname { get; set; } = string.Empty;
    public string SitePath { get; set; } = string.Empty;

    // Document library name, defaults to the site's default drive if empty.
    // Ignored when DriveId is set.
    public string DriveName { get; set; } = string.Empty;

    // Document library (drive) id, e.g. from Graph's /sites/{id}/drives.
    // When set, this is used directly and SiteHostname/SitePath/DriveName are skipped.
    public string DriveId { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        (!string.IsNullOrWhiteSpace(DriveId) || !string.IsNullOrWhiteSpace(SiteHostname));
}
