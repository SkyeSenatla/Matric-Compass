namespace API.Auth;

using API.Models;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

// What a successful login or refresh hands back to the controller: the
// access token for the body, and the raw refresh token for the cookie.
public record AuthResult(TokenResponse Tokens, string RefreshToken, DateTime RefreshTokenExpiresAt);

// Week 4 Day 2's rule still holds: the controller deals with HTTP (cookies,
// status codes); every DECISION about signing in lives here.
public class AuthService
{
    // Hashed once, at startup. Used when the email doesn't exist, so a wrong
    // email takes as long as a wrong password — otherwise response time alone
    // tells an attacker which emails have accounts.
    private static readonly string UnknownUserHash =
        new PasswordHasher<AppUser>().HashPassword(null!, "unknown-user-timing-equaliser");

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly TokenService _tokenService;
    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher<AppUser> passwordHasher,
        TokenService tokenService,
        IOptions<JwtOptions> options,
        TimeProvider clock)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _options = options.Value;
        _clock = clock;
    }

    private TimeSpan RefreshLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);
    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email);

        var result = user is null
            ? _passwordHasher.VerifyHashedPassword(null!, UnknownUserHash, request.Password)
            : _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        // ONE message for "no such email" and "wrong password". Telling them
        // apart tells an attacker which emails are worth guessing passwords for.
        if (user is null || result == PasswordVerificationResult.Failed)
            throw new AuthenticationFailedException("Email or password is incorrect.");

        var (rawToken, tokenHash) = _tokenService.CreateRefreshToken();
        var refreshToken = RefreshToken.StartFamily(user.Id, tokenHash, Now, RefreshLifetime);
        await _refreshTokens.AddAsync(refreshToken);

        return Issue(user, rawToken, refreshToken);
    }

    public async Task<AuthResult> RefreshAsync(string? rawRefreshToken)
    {
        if (string.IsNullOrEmpty(rawRefreshToken))
            throw new AuthenticationFailedException("No refresh token was sent.");

        var now = Now;
        var current = await _refreshTokens.GetByHashAsync(TokenService.HashRefreshToken(rawRefreshToken))
            ?? throw new AuthenticationFailedException("Refresh token is not recognised.");

        if (current.RevokedAt is not null)
        {
            // REUSE DETECTED. This token was already spent (rotated) or
            // revoked, yet someone just presented it again. Either the real
            // user's app replayed an old token, or an attacker stole one — the
            // server can't tell which. So the whole family dies, and the real
            // user simply signs in again. The attacker's copy is now worthless.
            await _refreshTokens.RevokeFamilyAsync(current.FamilyId, now);
            throw new AuthenticationFailedException("Refresh token was already used. Please sign in again.");
        }

        if (!current.IsActive(now))
            throw new AuthenticationFailedException("Refresh token has expired. Please sign in again.");

        var user = await _users.GetByIdAsync(current.UserId)
            ?? throw new AuthenticationFailedException("This account no longer exists.");

        // ROTATION: spend the old token, issue its successor, save both together.
        var (rawToken, tokenHash) = _tokenService.CreateRefreshToken();
        var successor = current.Rotate(tokenHash, now, RefreshLifetime);
        await _refreshTokens.SaveRotationAsync(successor);

        return Issue(user, rawToken, successor);
    }

    public async Task LogoutAsync(string? rawRefreshToken)
    {
        if (string.IsNullOrEmpty(rawRefreshToken))
            return; // already signed out — logout is idempotent

        var current = await _refreshTokens.GetByHashAsync(TokenService.HashRefreshToken(rawRefreshToken));
        if (current is not null)
            await _refreshTokens.RevokeFamilyAsync(current.FamilyId, Now);
    }

    private AuthResult Issue(AppUser user, string rawRefreshToken, RefreshToken refreshToken)
    {
        var access = _tokenService.CreateAccessToken(user);
        return new AuthResult(
            new TokenResponse(access.Token, access.ExpiresAt),
            rawRefreshToken,
            refreshToken.ExpiresAt);
    }
}
