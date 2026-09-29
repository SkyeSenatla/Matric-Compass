using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using API.Models;

namespace API.Tests;

// Demo 4: ErrorShapeTests only ever proves the API fails correctly. These
// tests prove the success shape holds through the full pipeline — model
// binding, validation, the service, the repository, serialization back out.
public class HappyPathTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HappyPathTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetStudents_returns_the_seeded_students()
    {
        var response = await _client.GetAsync("/api/students");

        response.EnsureSuccessStatusCode();
        var students = await response.Content.ReadFromJsonAsync<List<StudentResponse>>();
        Assert.Contains(students!, s => s.LearnerReferenceNumber == "LRN-2026-00114");
    }

    [Fact]
    public async Task CreateAptitudeTest_returns_201_with_a_Location_header_and_recommendations()
    {
        var students = await _client.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        var studentId = students!.First().Id;

        var response = await _client.PostAsJsonAsync("/api/aptitude-tests", new
        {
            studentId,
            testType = "Career Interest Inventory",
            dateTaken = DateTime.UtcNow,
            score = 92
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<AptitudeTestResponse>();
        Assert.Contains("Medicine", body!.RecommendedCareers);
    }

    [Fact]
    public async Task UpdateBursaryApplication_returns_204_and_the_change_is_visible_on_a_later_GET()
    {
        var students = await _client.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        var created = await _client.PostAsJsonAsync("/api/bursary-applications", new
        {
            studentId = students!.Last().Id,
            funder = "Thuthuka Bursary Fund",
            amount = 1000m,
            deadline = DateTime.UtcNow.AddMonths(3),
            requiredDocuments = Array.Empty<string>()
        });
        var application = await created.Content.ReadFromJsonAsync<BursaryApplicationResponse>();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/bursary-applications/{application!.Id}",
            new { amount = 2500m, deadline = DateTime.UtcNow.AddMonths(4) });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var reread = await _client.GetFromJsonAsync<BursaryApplicationResponse>(
            $"/api/bursary-applications/{application.Id}");
        Assert.Equal(2500m, reread!.Amount);
    }
}
