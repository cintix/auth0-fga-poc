namespace FGA_POC;

public class FgaSettings
{
    public string ApiUrl { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string AuthorizationModelId { get; set; } = string.Empty;

    public string ApiTokenIssuer { get; set; } = string.Empty;
    public string ApiAudience { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
