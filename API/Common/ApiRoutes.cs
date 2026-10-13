namespace API.Common;

// Week 6 Day 3: every URL this API publishes starts here. The version is
// part of the route BEFORE the first frontend ships — adding it after
// clients exist would break every URL they already call.
//
// URL-segment versioning: visible in the browser, in curl, in the logs and
// in yesterday's traces. When a breaking v2 arrives, the v2 controllers get
// a V2 prefix and v1 keeps working until it's retired.
public static class ApiRoutes
{
    public const string V1 = "api/v1";
}
