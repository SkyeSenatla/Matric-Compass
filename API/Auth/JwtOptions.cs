namespace API.Auth;

// Bound from the "Jwt" configuration section. Issuer, Audience and the two
// lifetimes live in appsettings.json (they aren't secret). SigningKey lives
// in user-secrets ONLY — anyone holding it can mint a valid Admin token.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 5;
    public int RefreshTokenDays { get; init; } = 7;
}
