namespace HouseHeroes.Mobile.Configuration;

public class AuthenticationConfig
{
    public const string SectionName = "EntraId";
    
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string UserFlow { get; set; } = "B2C_1_signup_signin";
    public string Authority { get; set; } = string.Empty;
    public string RedirectUri => $"msal{ClientId}://auth";
    public string[] Scopes { get; set; } = Array.Empty<string>();
}