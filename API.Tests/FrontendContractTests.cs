namespace API.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.Tests.TestSupport;
using Domain.Entities;

// Week 6 Day 3: the API as a BROWSER sees it — CORS, a rate-limited login,
// errors a form can use, and a response shaped for one screen.
[Collection("Postgres collection")]
public class FrontendContractTests
{
    private const string FrontendOrigin = "http://localhost:3000";

    private readonly PostgresApiFactory _factory;

    public FrontendContractTests(PostgresApiFactory factory) => _factory = factory;

    // What the browser sends before a JSON POST: "may I?"
    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }

    private async Task<Guid> GetStudentIdAsync(string learnerReferenceNumber)
    {
        var students = await _factory.CreateAdminClient().GetFromJsonAsync<JsonElement>("/api/v1/students");
        return students.EnumerateArray()
            .First(s => s.GetProperty("learnerReferenceNumber").GetString() == learnerReferenceNumber)
            .GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task A_preflight_from_the_frontend_is_allowed_with_credentials()
    {
        var response = await _factory.CreateClient().SendAsync(Preflight(FrontendOrigin));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(FrontendOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task A_preflight_from_any_other_origin_gets_no_cors_headers()
    {
        var response = await _factory.CreateClient().SendAsync(Preflight("https://evil.example"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Error_responses_carry_cors_headers_so_the_browser_can_read_them()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/students/{Guid.NewGuid()}");
        request.Headers.Add("Origin", FrontendOrigin);

        var response = await _factory.CreateAdminClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(FrontendOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Login_is_rate_limited_with_a_429_problem_and_retry_after()
    {
        // A copy of the app with a limit of 2, so the test needs 3 requests,
        // not 6 — and every other test keeps the factory's generous limit.
        await using var limitedApp = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:Login:PermitLimit", "2"));
        var client = limitedApp.CreateClient();

        HttpResponseMessage response = null!;
        for (var attempt = 1; attempt <= 3; attempt++)
            response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = "nobody@matric-compass.test", password = "not-the-password" });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.RetryAfter);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Too Many Requests", problem.GetProperty("title").GetString());
        Assert.True(problem.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task A_validation_failure_names_the_field_that_failed()
    {
        var response = await _factory.CreateAdminClient().PostAsJsonAsync("/api/v1/bursary-applications", new
        {
            studentId = await GetStudentIdAsync("LRN-2026-00114"),
            funder = "", // fails BursaryApplicationCreateRequestValidator
            amount = 100,
            deadline = DateTime.UtcNow.AddMonths(1),
            requiredDocuments = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEmpty(problem.GetProperty("errors").GetProperty("funder").EnumerateArray());
    }

    [Fact]
    public async Task A_number_sent_as_text_is_rejected()
    {
        var response = await _factory.CreateAdminClient().PostAsJsonAsync("/api/v1/bursary-applications", new
        {
            studentId = await GetStudentIdAsync("LRN-2026-00114"),
            funder = "Sasol Bursary",
            amount = "100", // a string: strict number handling says no
            deadline = DateTime.UtcNow.AddMonths(1),
            requiredDocuments = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("API.Models", body); // no internal type names
    }

    [Fact]
    public async Task A_learner_gets_their_own_dashboard_in_one_response()
    {
        var thandiweId = await GetStudentIdAsync("LRN-2026-00114");
        var student = await _factory.CreateAdminClient().GetFromJsonAsync<JsonElement>($"/api/v1/students/{thandiweId}");

        var dashboard = await _factory.CreateClientAs(Roles.Learner, thandiweId)
            .GetFromJsonAsync<JsonElement>("/api/v1/me/dashboard");

        Assert.Equal(student.GetProperty("fullName").GetString(), dashboard.GetProperty("fullName").GetString());
        Assert.Equal(student.GetProperty("subjects").GetArrayLength(), dashboard.GetProperty("subjects").GetArrayLength());
        var nsfas = dashboard.GetProperty("bursaryApplications").EnumerateArray()
            .First(application => application.GetProperty("funder").GetString() == "NSFAS");
        Assert.Equal(JsonValueKind.Number, nsfas.GetProperty("amountInCents").ValueKind);
        // Shaped for the screen: the LRN isn't shown, so it isn't sent.
        Assert.False(dashboard.TryGetProperty("learnerReferenceNumber", out _));
    }

    [Fact]
    public async Task Staff_get_403_from_the_learner_dashboard()
    {
        var response = await _factory.CreateAdminClient().GetAsync("/api/v1/me/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
