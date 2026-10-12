namespace API.Observability;

using System.Diagnostics;

// Week 6 Day 2: the ONE id that ties a request together — the error the
// user sees, every log line, and every span in the trace.
//
// Activity.Current is the request's trace span, which ASP.NET Core starts
// for every request. Its TraceId follows the W3C Trace Context standard:
// if the caller sent a "traceparent" header (a browser, another service),
// we continue THEIR trace instead of starting a new one.
public static class Correlation
{
    public static string GetId(HttpContext httpContext) =>
        Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
}
