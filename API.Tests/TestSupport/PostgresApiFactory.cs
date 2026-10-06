namespace API.Tests.TestSupport;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

// Replaces the manually-run dev Postgres container, for tests only. One
// container, started once for the whole test run (see the collection
// definition below), not one per test class — the seeding-race fix from
// Week 5 Day 1 (DisableTestParallelization) still matters here for exactly
// the same reason: one shared database, multiple test classes.
public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("matric_compass_test")
        .WithUsername("postgres")
        .WithPassword("devpassword")
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Overrides whatever's in user secrets / appsettings for the
            // test run only — the real dev Postgres is never touched by
            // a test, and a test run needs no manual setup step at all.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MatricCompass"] = _container.GetConnectionString()
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync() => await _container.DisposeAsync();
}

// xUnit collection fixture: every test class below shares ONE container
// instance and ONE Postgres database for the whole test run.
[CollectionDefinition("Postgres collection")]
public class PostgresCollection : ICollectionFixture<PostgresApiFactory> { }
