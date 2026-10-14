namespace API.Tests;

using API.Background;
using API.Tests.TestSupport;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Week 6 Day 4: the cleanup job, tested by running ONE pass directly —
// no waiting an hour for the timer. The tokens are written straight into the
// Testcontainers database with dates chosen to sit either side of the cutoff.
[Collection("Postgres collection")]
public class RefreshTokenCleanupTests
{
    private readonly PostgresApiFactory _factory;

    public RefreshTokenCleanupTests(PostgresApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Deletes_tokens_expired_past_the_grace_period_and_keeps_the_rest()
    {
        var now = DateTime.UtcNow;
        var lifetime = TimeSpan.FromDays(7);
        var longExpiredHash = $"cleanup-long-expired-{Guid.NewGuid()}";
        var recentlyExpiredHash = $"cleanup-recently-expired-{Guid.NewGuid()}";
        var activeHash = $"cleanup-active-{Guid.NewGuid()}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MatricCompassDbContext>();
            var userId = (await dbContext.Users.AsNoTracking().FirstAsync()).Id;

            dbContext.RefreshTokens.AddRange(
                // Expired 40 days ago: past the 30-day grace period, so it goes.
                RefreshToken.StartFamily(userId, longExpiredHash, now.AddDays(-47), lifetime),
                // Expired 5 days ago: still evidence for reuse detection, so it stays.
                RefreshToken.StartFamily(userId, recentlyExpiredHash, now.AddDays(-12), lifetime),
                // Still active: obviously stays.
                RefreshToken.StartFamily(userId, activeHash, now, lifetime));
            await dbContext.SaveChangesAsync();
        }

        // The very same instance the host started — AddHostedService
        // registered it as an IHostedService singleton.
        var job = _factory.Services.GetServices<IHostedService>().OfType<RefreshTokenCleanupService>().Single();
        var deleted = await job.RunOnceAsync(CancellationToken.None);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MatricCompassDbContext>();
            Assert.True(deleted >= 1);
            Assert.False(await dbContext.RefreshTokens.AnyAsync(t => t.TokenHash == longExpiredHash));
            Assert.True(await dbContext.RefreshTokens.AnyAsync(t => t.TokenHash == recentlyExpiredHash));
            Assert.True(await dbContext.RefreshTokens.AnyAsync(t => t.TokenHash == activeHash));
        }
    }

    [Fact]
    public async Task Running_twice_is_safe()
    {
        var job = _factory.Services.GetServices<IHostedService>().OfType<RefreshTokenCleanupService>().Single();

        await job.RunOnceAsync(CancellationToken.None);
        var secondRun = await job.RunOnceAsync(CancellationToken.None);

        // Nothing left to delete: the job is idempotent, so a second replica
        // running it at the same moment does no harm.
        Assert.Equal(0, secondRun);
    }
}
