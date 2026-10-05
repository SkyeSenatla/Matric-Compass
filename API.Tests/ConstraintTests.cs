namespace API.Tests;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using API.Common;
using API.Data;
using Domain.Entities;

// Week 5 Day 3: proves each database constraint holds ON ITS OWN — every
// test below deliberately goes around the C# check that normally stops the
// duplicate first, because the whole point of a constraint is the case
// where that check didn't run, or ran and was wrong.
public class ConstraintTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConstraintTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Second_active_application_with_the_same_funder_is_rejected_by_the_database()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MatricCompassDbContext>();
        var student = await dbContext.Students.AsNoTracking().FirstAsync(s => s.LearnerReferenceNumber == "LRN-2026-00114");
        var funder = $"Constraint Test Fund {Guid.NewGuid():N}";

        // Straight into the DbContext — BursaryApplicationService, and its
        // duplicate check, never runs.
        dbContext.BursaryApplications.Add(new BursaryApplication(student.Id, funder, 100m, DateTime.UtcNow.AddMonths(1), []));
        await dbContext.SaveChangesAsync();

        dbContext.BursaryApplications.Add(new BursaryApplication(student.Id, funder, 200m, DateTime.UtcNow.AddMonths(1), []));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("UX_BursaryApplications_StudentId_Funder_Active", postgres.ConstraintName);
    }

    [Fact]
    public async Task Duplicate_subject_is_rejected_by_the_database_when_EnrollSubject_cannot_see_it()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MatricCompassDbContext>();

        // Loaded WITHOUT .Include(s => s.Subjects) — so _subjects is empty and
        // EnrollSubject's "already enrolled?" check has nothing to compare
        // against. Thandiwe is already enrolled in MATH in the database.
        var thandiwe = await dbContext.Students.FirstAsync(s => s.LearnerReferenceNumber == "LRN-2026-00114");
        thandiwe.EnrollSubject("MATH", 75m); // passes the C# check...

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync()); // ...not the database's

        var postgres = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("UX_Subjects_StudentId_Code", postgres.ConstraintName);
    }

    [Fact]
    public async Task Unique_violation_becomes_409_problem_json_not_500()
    {
        // No HTTP route can reach the constraint without the service's check
        // stopping it first (that's the point of keeping both) — so this
        // drives DomainExceptionHandler directly with the exact exception
        // shape EF Core throws.
        var handler = new DomainExceptionHandler(NullLogger<DomainExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new DbUpdateException("save failed",
            new PostgresException("duplicate key value", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        Assert.Equal(409, body.GetProperty("status").GetInt32());
    }
}
