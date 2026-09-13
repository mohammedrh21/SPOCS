namespace SPOCS.Application.Contracts.Identity;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "SPOCS.API";
    public string Audience { get; set; } = "SPOCS.Client";
    public int ExpiryInMinutes { get; set; } = 10080; // 7 days default
}
