namespace API.Auth;

using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

public record AccessToken(string Token, DateTime ExpiresAt);

// Mints the two kinds of token. Holds no per-request state — only options
// and a clock — so it's registered as a Singleton.
public class TokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;
    private readonly SigningCredentials _signingCredentials;

    public TokenService(IOptions<JwtOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(AppUser user) =>
        CreateAccessToken(user.Id, user.Email, user.Role, user.StudentId);

    // The ACCESS token: a signed statement of who you are and what you are,
    // valid for minutes. The API trusts it without a database lookup — which
    // is exactly why it has to be short-lived: it can't be taken back.
    public AccessToken CreateAccessToken(Guid userId, string email, string role, Guid? studentId)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = userId.ToString(),
            [JwtRegisteredClaimNames.Email] = email,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [AppClaims.Role] = role
        };
        if (studentId is not null)
            claims[AppClaims.StudentId] = studentId.Value.ToString();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = _signingCredentials
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    // The REFRESH token: not a JWT at all, just 64 random bytes. It means
    // nothing on its own — only the server's database gives it meaning, and
    // that's what makes it revocable.
    public (string RawToken, string TokenHash) CreateRefreshToken()
    {
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (raw, HashRefreshToken(raw));
    }

    // SHA-256, not PasswordHasher: the input is 64 random bytes, not a
    // guessable password, so a slow hash buys nothing — and we need to look
    // the hash UP, which a salted password hash can't do.
    public static string HashRefreshToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
