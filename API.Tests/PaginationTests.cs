namespace API.Tests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using API.Tests.TestSupport;
using API.Models;

// Week 5 Day 3: the paging contract, tested as a contract — what a client
// can rely on, not how the repository happens to implement it.
[Collection("Postgres collection")]
public class PaginationTests
{
    private readonly HttpClient _client;

    public PaginationTests(PostgresApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Walking_every_page_returns_each_application_exactly_once_then_an_empty_token()
    {
        // A brand-new student, so this test controls exactly which rows match
        // its filter — no matter what other tests (or old runs) left behind.
        var studentResponse = await _client.PostAsJsonAsync("/api/students",
            new { fullName = "Paging Test Student", learnerReferenceNumber = $"LRN-PAGE-{Guid.NewGuid():N}" });
        var student = (await studentResponse.Content.ReadFromJsonAsync<StudentResponse>())!;

        var createdIds = new List<Guid>();
        for (var i = 1; i <= 5; i++)
        {
            var created = await _client.PostAsJsonAsync("/api/bursary-applications", new
            {
                studentId = student.Id, funder = $"Paging Fund {i}", amount = 1000m * i,
                deadline = DateTime.UtcNow.Date.AddDays(10 + i), requiredDocuments = Array.Empty<string>()
            });
            createdIds.Add((await created.Content.ReadFromJsonAsync<BursaryApplicationResponse>())!.Id);
        }

        var seen = new List<BursaryApplicationResponse>();
        var token = "";
        var pages = 0;
        do
        {
            var page = await _client.GetFromJsonAsync<PagedResponse<BursaryApplicationResponse>>(
                $"/api/bursary-applications?studentId={student.Id}&pageSize=2&pageToken={token}");
            seen.AddRange(page!.Items);
            token = page.NextPageToken;
            pages++;
        } while (token != "");

        Assert.Equal(3, pages);                                                    // 2 + 2 + 1
        Assert.Equal(createdIds.Order(), seen.Select(a => a.Id).Order());          // all five, none twice
        Assert.Equal(seen.OrderBy(a => a.Deadline).Select(a => a.Id), seen.Select(a => a.Id)); // in deadline order

        // Cleanup: deleting the student cascades to its applications.
        await _client.DeleteAsync($"/api/students/{student.Id}");
    }

    [Fact]
    public async Task Page_size_above_the_maximum_is_reduced_not_rejected()
    {
        var response = await _client.GetAsync("/api/bursary-applications?pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<BursaryApplicationResponse>>();
        Assert.True(page!.Items.Count <= API.Services.BursaryApplicationService.MaxPageSize);
    }

    [Theory]
    [InlineData("pageSize=-1")]
    [InlineData("orderBy=funder")]        // not on the allow-list
    [InlineData("status=Pending")]        // not a real status
    [InlineData("pageToken=not-a-token")] // malformed
    public async Task Invalid_paging_input_returns_400_problem_json(string query)
    {
        var response = await _client.GetAsync($"/api/bursary-applications?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Reusing_a_token_with_a_different_sort_returns_400()
    {
        var first = await _client.GetFromJsonAsync<PagedResponse<BursaryApplicationResponse>>(
            "/api/bursary-applications?pageSize=1&orderBy=deadline");
        Assert.NotEqual("", first!.NextPageToken); // the seeded data guarantees a second page

        var response = await _client.GetAsync(
            $"/api/bursary-applications?pageSize=1&orderBy=amount&pageToken={first.NextPageToken}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
