namespace API.Models;

public record LoginRequest(string Email, string Password);

// The access token goes in the BODY (the frontend keeps it in memory).
// The refresh token is NOT here — it travels only in an HttpOnly cookie.
public record TokenResponse(string AccessToken, DateTime ExpiresAt);
