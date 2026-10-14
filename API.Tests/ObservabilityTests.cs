namespace API.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.Tests.TestSupport;

// Week 6 Day 2: the parts of observability a client can see — health
// endpoints a platform can probe without a token, and a correlation id on
// every error that is the SAME id as the caller's W3C trace.
[Collection("Postgres collection")]
public class ObservabilityTests
{
    // A W3C traceparent header: version - trace id - parent span id - flags.
    // Hand-made here so the test knows exactly which trace id to expect back.
    private const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
    private const string TraceParent = $"00-{TraceId}-00f067aa0ba902b7-01";

    private readonly PostgresApiFactory _factory;

    public ObservabilityTests(PostgresApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Liveness_answers_without_a_token()
    {
        var response = await _factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Readiness_checks_the_database_and_answers_without_a_token()
    {
        var response = await _factory.CreateClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task An_error_from_our_handler_carries_the_callers_trace_id()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/students/{Guid.NewGuid()}");
        request.Headers.Add("traceparent", TraceParent);

        var response = await _factory.CreateAdminClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(TraceId, problem.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task An_error_the_framework_writes_carries_the_same_trace_id()
    {
        // No token: this 401 comes from the authorization middleware, not from
        // DomainExceptionHandler — CustomizeProblemDetails is what adds the id.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/students");
        request.Headers.Add("traceparent", TraceParent);

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(TraceId, problem.GetProperty("correlationId").GetString());
    }
}
