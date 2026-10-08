using API.Auth;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// Week 6 Day 1: the only controller anyone can call without a token —
// you can't be asked for a token on the endpoint that gives you one.
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    // Scoped to /api/auth: the browser only sends this cookie to the three
    // endpoints below, never with ordinary API calls.
    private const string RefreshCookieName = "refresh_token";

    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    /// <summary>Signs in with email and password.</summary>
    /// <response code="200">Signed in. The refresh token is set as an HttpOnly cookie.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> LoginAsync(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(result.Tokens);
    }

    /// <summary>Swaps the refresh-token cookie for a new access token and a new refresh token.</summary>
    /// <response code="200">Rotated. The old refresh token can never be used again.</response>
    /// <response code="401">Missing, unknown, expired or already-used refresh token.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> RefreshAsync()
    {
        var result = await _authService.RefreshAsync(Request.Cookies[RefreshCookieName]);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(result.Tokens);
    }

    /// <summary>Signs out: revokes this session's refresh tokens and clears the cookie.</summary>
    /// <response code="204">Signed out (also when already signed out).</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync()
    {
        await _authService.LogoutAsync(Request.Cookies[RefreshCookieName]);
        Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions(expires: null));
        return NoContent();
    }

    private void SetRefreshCookie(string rawToken, DateTime expiresAt) =>
        Response.Cookies.Append(RefreshCookieName, rawToken, RefreshCookieOptions(expiresAt));

    // HttpOnly: JavaScript can't read it, so an XSS bug can't steal it.
    // Secure:   only ever sent over HTTPS.
    // SameSite=Strict: never sent on a request another site triggers (CSRF).
    // Path:     only sent to /api/auth/*.
    private static CookieOptions RefreshCookieOptions(DateTime? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
        Expires = expires
    };
}
