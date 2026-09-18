namespace DsacReporting.Api.Services;

public class AnthropicSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
