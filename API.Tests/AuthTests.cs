namespace API.Tests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using API.Models;
using API.Tests.TestSupport;
using Domain.Entities;

// Week 6 Day 1: authentication (who are you?) and authorization (what may
// you do?), tested as behaviour a client can observe — status codes and
// cookies, not the internals of TokenService.
[Collection("Postgres collection")]
public class AuthTests
{
    private const string LearnerEmail = "thandiwe@matric-compass.test";

    private readonly PostgresApiFactory _factory;
    private readonly HttpClient _anonymous;

    public AuthTests(PostgresApiFactory factory)
    {
        _factory = factory;
        // https, so the Secure refresh cookie behaves as it will in production.
        // HandleCookies = false: these tests read and send the cookie by hand,
        // because replaying an OLD cookie is exactly what one of them tests.
        _anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });
    }

    private async Task<Guid> GetStudentIdAsync(string learnerReferenceNumber)
    {
        var students = await _factory.CreateAdminClient()
            .GetFromJsonAsync<List<StudentResponse>>("/api/students");
        return students!.Single(s => s.LearnerReferenceNumber == learnerReferenceNumber).Id;
    }

    private static string ReadRefreshCookie(HttpResponseMessage response)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(h => h.StartsWith("refresh_token="));
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        return setCookie.Split(';')[0]["refresh_token=".Length..];
    }

    private Task<HttpResponseMessage> RefreshAsync(string rawRefreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={rawRefreshToken}");
        return _anonymous.SendAsync(request);
    }

    [Fact]
    public async Task No_token_is_401_problem_json()
    {
        var response = await _anonymous.GetAsync("/api/students");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_learner_is_signed_in_but_not_allowed_to_list_every_student_403()
    {
        var learner = _factory.CreateClientAs(Roles.Learner, await GetStudentIdAsync("LRN-2026-00114"));

        var response = await learner.GetAsync("/api/students");

        // 401 = "who are you?"  403 = "I know who you are — and no."
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_learner_sees_their_own_record_and_another_learners_record_is_404()
    {
        var thandiweId = await GetStudentIdAsync("LRN-2026-00114");
        var siphoId = await GetStudentIdAsync("LRN-2026-00287");
        var thandiwe = _factory.CreateClientAs(Roles.Learner, thandiweId);

        var own = await thandiwe.GetAsync($"/api/students/{thandiweId}");
        var someoneElses = await thandiwe.GetAsync($"/api/students/{siphoId}");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, someoneElses.StatusCode); // not 403: don't confirm it exists
    }

    [Fact]
    public async Task Only_an_admin_can_delete_a_student()
    {
        var siphoId = await GetStudentIdAsync("LRN-2026-00287");
        var counsellor = _factory.CreateClientAs(Roles.Counsellor);

        var response = await counsellor.DeleteAsync($"/api/students/{siphoId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_is_401_and_says_nothing_about_which_part_was_wrong()
    {
        var wrongPassword = await _anonymous.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(LearnerEmail, "not-the-password"));
        var unknownEmail = await _anonymous.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody@matric-compass.test", "not-the-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(
            (await wrongPassword.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!.Detail,
            (await unknownEmail.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!.Detail);
    }

    [Fact]
    public async Task Login_then_the_access_token_works_on_a_protected_endpoint()
    {
        var login = await _anonymous.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(LearnerEmail, PostgresApiFactory.SeedPassword));
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<TokenResponse>();
        ReadRefreshCookie(login); // asserts the cookie flags

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/students/{await GetStudentIdAsync("LRN-2026-00114")}");
        request.Headers.Authorization = new("Bearer", tokens!.AccessToken);
        var response = await _anonymous.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_and_reusing_a_spent_token_revokes_the_whole_family()
    {
        var login = await _anonymous.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(LearnerEmail, PostgresApiFactory.SeedPassword));
        login.EnsureSuccessStatusCode();
        var first = ReadRefreshCookie(login);

        // Normal use: the first refresh token is swapped for a second.
        var rotated = await RefreshAsync(first);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var second = ReadRefreshCookie(rotated);
        Assert.NotEqual(first, second);

        // Someone replays the FIRST (already spent) token: rejected...
        var replay = await RefreshAsync(first);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // ...and the SECOND token, never used, is now dead too.
        var afterReuse = await RefreshAsync(second);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var login = await _anonymous.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(LearnerEmail, PostgresApiFactory.SeedPassword));
        var refreshToken = ReadRefreshCookie(login);

        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        Assert.Equal(HttpStatusCode.NoContent, (await _anonymous.SendAsync(logout)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refreshToken)).StatusCode);
    }
}
