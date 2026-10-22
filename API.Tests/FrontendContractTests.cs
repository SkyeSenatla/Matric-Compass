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
            amountInCents = 10_000,
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
            amountInCents = "10000", // a string: strict number handling says no
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

    // Week 7 Day 3: the counsellor's list reads this endpoint directly.
    [Fact]
    public async Task Bursary_applications_send_money_as_integer_cents()
    {
        var page = await _factory.CreateClientAs(Roles.Counsellor)
            .GetFromJsonAsync<JsonElement>("/api/v1/bursary-applications?pageSize=50");

        var nsfas = page.GetProperty("items").EnumerateArray()
            .First(application => application.GetProperty("funder").GetString() == "NSFAS");
        Assert.Equal(4_500_000, nsfas.GetProperty("amountInCents").GetInt64()); // R45 000,00
        Assert.False(nsfas.TryGetProperty("amount", out _));
    }

    [Theory]
    [InlineData(Roles.Learner)]
    [InlineData(Roles.Counsellor)]
    [InlineData(Roles.Admin)]
    public async Task Anyone_signed_in_can_ask_who_they_are(string role)
    {
        var studentId = role == Roles.Learner ? await GetStudentIdAsync("LRN-2026-00114") : (Guid?)null;

        var me = await _factory.CreateClientAs(role, studentId).GetFromJsonAsync<JsonElement>("/api/v1/me");

        Assert.Equal(role, me.GetProperty("role").GetString());
        Assert.False(string.IsNullOrEmpty(me.GetProperty("email").GetString()));
    }

    [Fact]
    public async Task Who_am_I_needs_a_token()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Week 7 Day 4: the bursary form sends what a person typed, converted to
    // cents. R19,99 is the classic float trap (19.99 * 100 is
    // 1998.9999999999998 in JavaScript), so it's the amount that proves cents
    // survive the round trip exactly.
    [Fact]
    public async Task Creating_an_application_takes_money_as_integer_cents()
    {
        var response = await _factory.CreateClientAs(Roles.Counsellor).PostAsJsonAsync("/api/v1/bursary-applications", new
        {
            studentId = await GetStudentIdAsync("LRN-2026-00287"),
            funder = $"Cents Fund {Guid.NewGuid():N}",
            amountInCents = 1_999, // R19,99
            deadline = DateTime.UtcNow.AddMonths(1),
            requiredDocuments = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1_999, created.GetProperty("amountInCents").GetInt64());
    }

    // Week 7 Day 4: the form shows errors["funder"] next to the funder field,
    // so the key and the message are both part of the contract.
    [Fact]
    public async Task A_funder_over_100_characters_is_a_400_on_the_funder_field()
    {
        var response = await _factory.CreateClientAs(Roles.Counsellor).PostAsJsonAsync("/api/v1/bursary-applications", new
        {
            studentId = await GetStudentIdAsync("LRN-2026-00287"),
            funder = new string('A', 101),
            amountInCents = 10_000,
            deadline = DateTime.UtcNow.AddMonths(1),
            requiredDocuments = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var messages = problem.GetProperty("errors").GetProperty("funder").EnumerateArray().Select(m => m.GetString());
        Assert.Contains("Funder names are 100 characters or fewer.", messages);
    }

    // Week 7 Day 4: the frontend maps errors["amountInCents"] onto its
    // amount field. If this key changed, the message would land nowhere.
    [Fact]
    public async Task A_zero_amount_is_reported_under_amountInCents()
    {
        var response = await _factory.CreateClientAs(Roles.Counsellor).PostAsJsonAsync("/api/v1/bursary-applications", new
        {
            studentId = await GetStudentIdAsync("LRN-2026-00287"),
            funder = $"Zero Fund {Guid.NewGuid():N}",
            amountInCents = 0,
            deadline = DateTime.UtcNow.AddMonths(1),
            requiredDocuments = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("amountInCents", out _));
    }

    // Week 7 Day 4: <input type="date"> sends a date with no time zone. This
    // was a 500 (Npgsql won't save an Unspecified DateTime) until
    // UtcDateTimeConverter.
    [Fact]
    public async Task A_date_only_deadline_is_accepted_as_that_date_in_UTC()
    {
        var date = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-dd");

        var response = await _factory.CreateClientAs(Roles.Counsellor).PostAsJsonAsync("/api/v1/bursary-applications", new
        {
            studentId = await GetStudentIdAsync("LRN-2026-00287"),
            funder = $"Date Fund {Guid.NewGuid():N}",
            amountInCents = 10_000,
            deadline = date,
            requiredDocuments = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"{date}T00:00:00Z", created.GetProperty("deadline").GetString());
    }

    [Fact]
    public async Task Staff_get_403_from_the_learner_dashboard()
    {
        var response = await _factory.CreateAdminClient().GetAsync("/api/v1/me/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
