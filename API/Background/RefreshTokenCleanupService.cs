namespace API.Background;

using System.Diagnostics;
using API.Observability;
using Domain.Repositories;

// Week 6 Day 4: Day 1 adds a refresh-token row on every login and every
// refresh, and nothing ever removes one. This job deletes tokens that expired
// more than RetainExpiredFor ago, once per Interval, for as long as the API runs.
//
// Why keep expired tokens for a while at all? Reuse detection (AuthService)
// recognises a SPENT token being replayed and revokes its whole family.
// Delete a token the moment it expires and a replay of it is merely
// "unknown" — the alarm never rings. The grace period keeps the evidence.
public sealed class RefreshTokenCleanupService : BackgroundService
{
    public const string ActivitySourceName = "MatricCompass.Jobs";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _clock;
    private readonly MatricCompassMetrics _metrics;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly RefreshTokenCleanupOptions _options;

    public RefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        MatricCompassMetrics metrics,
        ILogger<RefreshTokenCleanupService> logger,
        RefreshTokenCleanupOptions options)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _metrics = metrics;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PeriodicTimer + TimeProvider: one tick per Interval, and testable.
        using var timer = new PeriodicTimer(_options.Interval, _clock);
        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The app is shutting down: stop quietly.
        }
    }

    // Public so a test can run exactly one pass without waiting for the timer.
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        // No request means no automatic trace — so the job starts its own span.
        using var activity = ActivitySource.StartActivity("refresh-token-cleanup");
        try
        {
            // This service is a SINGLETON; the repository holds a SCOPED
            // DbContext. A fresh scope per run gives each run its own
            // DbContext, disposed when the run ends (Week 5's lifetime rule).
            await using var scope = _scopeFactory.CreateAsyncScope();
            var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            var cutoff = _clock.GetUtcNow().UtcDateTime - _options.RetainExpiredFor;
            var deleted = await refreshTokens.DeleteExpiredBeforeAsync(cutoff, cancellationToken);

            activity?.SetTag("matric_compass.deleted_count", deleted);
            _metrics.RefreshTokensDeleted(deleted);
            _logger.LogInformation(
                "Refresh token cleanup deleted {DeletedCount} tokens that expired before {Cutoff:O}", deleted, cutoff);
            return deleted;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // An exception escaping ExecuteAsync STOPS THE WHOLE APP (the
            // .NET 6+ default). One failed run — Postgres restarting, say —
            // must not take the API down with it: log it and try next time.
            activity?.SetStatus(ActivityStatusCode.Error);
            _logger.LogError(exception, "Refresh token cleanup failed; it will run again in {Interval}", _options.Interval);
            return 0;
        }
    }
}

// How often to run, and how long an expired token is kept before deletion.
public sealed record RefreshTokenCleanupOptions(TimeSpan Interval, TimeSpan RetainExpiredFor);
