namespace API.Tests.TestSupport;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using System.Net.Http.Headers;
using API.Auth;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

// Replaces the manually-run dev Postgres container, for tests only. One
// container, started once for the whole test run (see the collection
// definition below), not one per test class — the seeding-race fix from
// Week 5 Day 1 (DisableTestParallelization) still matters here for exactly
// the same reason: one shared database, multiple test classes.
public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Week 6 Day 1: test-only secrets. Real ones live in user-secrets; these
    // exist only inside this test run, like the container itself.
    public const string SeedPassword = "Test-Seed-Password-123!";
    private const string TestSigningKey = "test-only-signing-key-that-is-at-least-32-bytes";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("matric_compass_test")
        .WithUsername("postgres")
        .WithPassword("devpassword")
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting, NOT ConfigureAppConfiguration. Program.cs reads these
        // values immediately, at startup — and with minimal hosting, sources
        // added through ConfigureAppConfiguration only arrive later, when the
        // host is built. By then Program.cs has already read the REAL values
        // from user-secrets. UseSetting is applied before Program.cs runs.
        builder.UseSetting("ConnectionStrings:MatricCompass", _container.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
        builder.UseSetting("SeedUsers:Password", SeedPassword);
    }

    async Task IAsyncLifetime.DisposeAsync() => await _container.DisposeAsync();

    // Week 6 Day 1: an HttpClient that's already signed in as the given role.
    // It mints the token with the app's OWN TokenService, so it's signed with
    // the same key and carries the same claims a real login would — no
    // password round trip needed in tests that aren't about logging in.
    public HttpClient CreateClientAs(string role, Guid? studentId = null)
    {
        var client = CreateClient();
        var tokenService = Services.GetRequiredService<TokenService>();
        var token = tokenService.CreateAccessToken(
            Guid.NewGuid(), $"{role.ToLowerInvariant()}@tests.matric-compass.test", role, studentId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }

    // Every pre-Week-6 test was written for "can do anything". Admin keeps it that way.
    public HttpClient CreateAdminClient() => CreateClientAs(Roles.Admin);
}

// xUnit collection fixture: every test class below shares ONE container
// instance and ONE Postgres database for the whole test run.
[CollectionDefinition("Postgres collection")]
public class PostgresCollection : ICollectionFixture<PostgresApiFactory> { }
