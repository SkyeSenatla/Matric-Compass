using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace API.Tests;

// Demo 7: proves "one failure shape" is a claim the build can fail on, not
// just a talking point. Each case maps to one branch of
// DomainExceptionHandler's switch (API/Common/DomainExceptionHandler.cs) —
// filling them in was a matter of calling the same endpoints exercised
// live in Demo 0, not new logic to design.
public class ErrorShapeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ErrorShapeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> GetSeededStudentIdAsync(string learnerReferenceNumber)
    {
        var students = await _client.GetFromJsonAsync<JsonElement>("/api/students");
        var match = students.EnumerateArray()
            .First(s => s.GetProperty("learnerReferenceNumber").GetString() == learnerReferenceNumber);
        return match.GetProperty("id").GetGuid();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string exceptionTypeName)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.EndsWith(exceptionTypeName, body.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Malformed_request_returns_problem_json_400()
    {
        var studentId = await GetSeededStudentIdAsync("LRN-2026-00114");

        var response = await _client.PostAsJsonAsync("/api/bursary-applications", new
        {
            studentId,
            funder = "", // blank funder — fails BursaryApplicationCreateRequestValidator.RuleFor(x => x.Funder)
            amount = 100,
            deadline = DateTime.UtcNow.AddMonths(1),
            requiredDocuments = Array.Empty<string>()
        });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "ValidationException");
    }

    [Fact]
    public async Task Duplicate_active_application_returns_409()
    {
        // Thandiwe (LRN-2026-00114) is seeded in Program.cs with an active
        // NSFAS application — a second NSFAS application for her hits
        // BursaryApplicationService's duplicate-active-application rule.
        var studentId = await GetSeededStudentIdAsync("LRN-2026-00114");

        var response = await _client.PostAsJsonAsync("/api/bursary-applications", new
        {
            studentId,
            funder = "NSFAS",
            amount = 100,
            deadline = DateTime.UtcNow.AddMonths(1),
            requiredDocuments = Array.Empty<string>()
        });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ConflictException");
    }

    [Fact]
    public async Task Deadline_in_past_returns_422()
    {
        var studentId = await GetSeededStudentIdAsync("LRN-2026-00287");

        var response = await _client.PostAsJsonAsync("/api/bursary-applications", new
        {
            studentId,
            funder = "Funza Lushaka Top-Up", // a funder this student has no active application with
            amount = 100,
            deadline = DateTime.UtcNow.AddDays(-1), // in the past
            requiredDocuments = Array.Empty<string>()
        });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "UnprocessableEntityException");
    }

    [Fact]
    public async Task Unknown_id_returns_404()
    {
        var response = await _client.GetAsync($"/api/bursary-applications/{Guid.Empty}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "NotFoundException");
    }
}
