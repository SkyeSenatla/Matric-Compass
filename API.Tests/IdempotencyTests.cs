using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using API.Models;

namespace API.Tests;

// Demo 5: idempotent doesn't mean "always returns the same status code" —
// it means "calling it twice leaves the system in the same state as calling
// it once." DELETE and PUT behave differently under a second call, and the
// boundary tests below exist because >= 85, InclusiveBetween(0, 100), and
// < DateTime.UtcNow.Date are exactly the kind of off-by-one-prone conditions
// that read correctly and still hide a < where a <= belongs.
public class IdempotencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IdempotencyTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Deleting_the_same_student_twice_is_204_then_404_but_idempotent_in_effect()
    {
        var created = await _client.PostAsJsonAsync("/api/students",
            new { fullName = "Palesa Mokoena", learnerReferenceNumber = $"LRN-TEST-{Guid.NewGuid():N}" });
        var student = await created.Content.ReadFromJsonAsync<StudentResponse>();

        var firstDelete = await _client.DeleteAsync($"/api/students/{student!.Id}");
        var secondDelete = await _client.DeleteAsync($"/api/students/{student.Id}");

        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);  // deleted
        Assert.Equal(HttpStatusCode.NotFound, secondDelete.StatusCode);  // already gone

        // The status code changed between calls — the END STATE didn't: the
        // student is gone either way. THAT'S the idempotency guarantee DELETE
        // makes, not "always 204."
        var getAfter = await _client.GetAsync($"/api/students/{student.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfter.StatusCode);
    }

    [Fact]
    public async Task Updating_the_same_bursary_application_twice_with_the_same_payload_is_204_both_times()
    {
        // PUT is genuinely idempotent: same request, same effect, same status,
        // as many times as you send it — unlike DELETE above.
        //
        // Week 5 Day 3: every update now carries the Version it was based on,
        // and after the first PUT that version is stale. The second PUT still
        // gets 204, because it changes nothing: EF Core sends no UPDATE at all
        // when no property changed, so there is no stale write to reject.
        // (ConcurrencyTests covers a stale PUT that DOES change something: 409.)
        //
        // The deadline is a whole date on purpose. DateTime.UtcNow has
        // 100-nanosecond precision; Postgres stores microseconds. Send UtcNow
        // twice and the "same" deadline is really a different value from the
        // stored one — a real change — so the second PUT becomes a stale write
        // and gets 409. An identical payload has to be identical after the
        // round trip, not just in the C# source.
        var deadline = DateTime.UtcNow.Date.AddMonths(2);
        var students = await _client.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        var created = await _client.PostAsJsonAsync("/api/bursary-applications", new
        {
            // Unique per run — see HappyPathTests' bursary update test.
            studentId = students!.First().Id, funder = $"MTN Bursary {Guid.NewGuid():N}", amount = 1000m,
            deadline, requiredDocuments = Array.Empty<string>()
        });
        var application = await created.Content.ReadFromJsonAsync<BursaryApplicationResponse>();
        var url = $"/api/bursary-applications/{application!.Id}";
        var payload = new { amount = 1500m, deadline, version = application.Version };

        var first = await _client.PutAsJsonAsync(url, payload);
        var second = await _client.PutAsJsonAsync(url, payload);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var after = await _client.GetFromJsonAsync<BursaryApplicationResponse>(url);
        Assert.Equal(1500m, after!.Amount);
    }

    [Theory]
    [InlineData(0)]   // lower boundary — must pass
    [InlineData(100)] // upper boundary — must pass
    public async Task AptitudeTest_score_at_the_inclusive_boundary_is_accepted(int score)
    {
        var students = await _client.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        var response = await _client.PostAsJsonAsync("/api/aptitude-tests", new
        {
            studentId = students!.First().Id, testType = "Boundary Check",
            dateTaken = DateTime.UtcNow, score
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task AptitudeTest_score_outside_the_boundary_returns_400_problem_json(int score)
    {
        // FluentValidation's InclusiveBetween(0, 100) rejects these before the
        // AptitudeTest constructor's own guard clause ever runs — this test is
        // pinned to the validator's behavior, not the entity's, on purpose.
        var students = await _client.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        var response = await _client.PostAsJsonAsync("/api/aptitude-tests", new
        {
            studentId = students!.First().Id, testType = "Boundary Check",
            dateTaken = DateTime.UtcNow, score
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BursaryApplication_deadline_of_exactly_today_is_not_in_the_past()
    {
        // request.Deadline.Date < DateTime.UtcNow.Date — today fails "<", so
        // today is accepted. A test that only tried yesterday and tomorrow
        // would miss this exact boundary silently passing either way.
        var students = await _client.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        var response = await _client.PostAsJsonAsync("/api/bursary-applications", new
        {
            studentId = students!.Last().Id, funder = $"Today Boundary Fund {Guid.NewGuid():N}", amount = 500m,
            deadline = DateTime.UtcNow.Date, requiredDocuments = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
