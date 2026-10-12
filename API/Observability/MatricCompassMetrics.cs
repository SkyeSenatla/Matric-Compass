namespace API.Observability;

using System.Diagnostics.Metrics;

// Week 6 Day 2: our own metrics, for things the framework can't see.
// ASP.NET Core already measures every request's duration and status code;
// it has no idea what a "sign-in" or a "stolen refresh token" is.
//
// Singleton: a Meter and its instruments are created once and shared.
public sealed class MatricCompassMetrics
{
    public const string MeterName = "MatricCompass";

    private readonly Counter<long> _signIns;
    private readonly Counter<long> _refreshTokenReuse;

    public MatricCompassMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _signIns = meter.CreateCounter<long>(
            "matric_compass.auth.sign_ins",
            unit: "{attempt}",
            description: "Sign-in attempts, tagged by outcome.");

        _refreshTokenReuse = meter.CreateCounter<long>(
            "matric_compass.auth.refresh_token_reuse",
            unit: "{event}",
            description: "Times a spent refresh token was presented again.");
    }

    // Tags are counted per distinct value, so they must come from a small,
    // fixed set ("success"/"failure"). Never tag a metric with an email or an
    // id: that leaks personal information AND creates one series per user.
    public void SignInAttempted(bool succeeded) =>
        _signIns.Add(1, new KeyValuePair<string, object?>("outcome", succeeded ? "success" : "failure"));

    public void RefreshTokenReuseDetected() => _refreshTokenReuse.Add(1);
}
