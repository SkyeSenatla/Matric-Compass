namespace API.Tests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using API.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using API.Models;

// Week 5 Day 3: proves a conflicting write is REJECTED, not silently applied.
// No Thread.Sleep, no Task.Delay, no real parallelism — concurrency is about
// the ORDER two writes interleave in, and a test can just perform them in
// the bad order on purpose. That makes it deterministic.
[Collection("Postgres collection")]
public class ConcurrencyTests
{
    private readonly PostgresApiFactory _factory;
    private readonly HttpClient _client;

    public ConcurrencyTests(PostgresApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAdminClient();
    }

    private async Task<BursaryApplicationResponse> CreateApplicationAsync()
    {
        var students = await _client.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        var created = await _client.PostAsJsonAsync("/api/bursary-applications", new
        {
            studentId = students!.First().Id,
            funder = $"Concurrency Test Fund {Guid.NewGuid():N}",
            amount = 1000m,
            deadline = DateTime.UtcNow.AddMonths(2),
            requiredDocuments = Array.Empty<string>()
        });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<BursaryApplicationResponse>())!;
    }

    [Fact]
    public async Task Second_writer_with_a_stale_xmin_gets_DbUpdateConcurrencyException()
    {
        var application = await CreateApplicationAsync();

        // TWO scopes = two DbContexts = two independent "users". One context
        // can never conflict with itself: ask it for the same row twice and
        // its identity map hands back the SAME object both times.
        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var counsellorA = scopeA.ServiceProvider.GetRequiredService<MatricCompassDbContext>();
        var counsellorB = scopeB.ServiceProvider.GetRequiredService<MatricCompassDbContext>();

        // Both read the same row, at the same xmin.
        var seenByA = await counsellorA.BursaryApplications.FirstAsync(b => b.Id == application.Id);
        var seenByB = await counsellorB.BursaryApplications.FirstAsync(b => b.Id == application.Id);

        // A saves first — Postgres writes a new row version with a new xmin.
        seenByA.UpdateDetails(2000m, seenByA.Deadline);
        await counsellorA.SaveChangesAsync();

        // B saves second, still holding the OLD xmin. Without a concurrency
        // token this would silently overwrite A's amount.
        seenByB.UpdateDetails(seenByB.Amount, seenByB.Deadline.AddDays(7));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => counsellorB.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_with_a_stale_version_returns_409_problem_json()
    {
        var application = await CreateApplicationAsync();
        var url = $"/api/bursary-applications/{application.Id}";

        // First edit, based on the version from the original read: succeeds.
        var first = await _client.PutAsJsonAsync(url,
            new { amount = 1500m, deadline = application.Deadline, version = application.Version });
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        // Second edit, based on that SAME (now stale) read: rejected.
        var second = await _client.PutAsJsonAsync(url,
            new { amount = 1750m, deadline = application.Deadline, version = application.Version });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);

        // And the first edit is still the one that stuck.
        var reread = await _client.GetFromJsonAsync<BursaryApplicationResponse>(url);
        Assert.Equal(1500m, reread!.Amount);
    }
}
