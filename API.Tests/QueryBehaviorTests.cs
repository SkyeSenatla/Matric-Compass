namespace API.Tests;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;

// Week 5 Day 2, Demo 6: an explicitly temporary demonstration, not a
// regression test. Run with:
//   dotnet test --filter QueryBehaviorTests -l "console;verbosity=detailed"
// and count the "Executed DbCommand" lines under each banner — Program.cs's
// LogTo() prints one per SQL command.
public class QueryBehaviorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public QueryBehaviorTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Naive_loop_triggers_one_query_per_student()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MatricCompassDbContext>();

        Console.WriteLine("=== NAIVE: watch the query count below ===");
        var students = await dbContext.Students.AsNoTracking().ToListAsync(); // query 1

        foreach (var student in students)
        {
            // No Include anywhere above — each access below is its OWN
            // round trip, once per student, because BursaryApplications
            // was never asked for as part of the first query.
            var count = await dbContext.BursaryApplications
                .Where(b => b.StudentId == student.Id)
                .CountAsync(); // query 2..N+1
        }
    }

    [Fact]
    public async Task Include_collapses_the_same_result_into_one_query()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MatricCompassDbContext>();

        Console.WriteLine("=== FIXED: watch the query count below ===");
        var students = await dbContext.Students.AsNoTracking()
            .Include(s => s.BursaryApplications)
            .ToListAsync(); // ONE query
    }
}
