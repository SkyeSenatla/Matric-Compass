namespace API.Common;

using System.Text.Json;
using Domain.Repositories;
using Domain.Entities;

// AIP-158: the page token is OPAQUE. The client receives it, stores it, and
// sends it back untouched — it never builds one, and it never needs to know
// whether we page by offset or by keyset underneath. That's what lets us
// change the implementation later without breaking a single client.
//
// The token also records the filter and sort it was issued for. Reusing it
// with a different filter or sort is a client error (400), not a silently
// wrong page.
public static class PageToken
{
    private record Payload(
        Guid? StudentId, BursaryApplicationStatus? Status, BursaryApplicationSort OrderBy,
        long DeadlineTicks, decimal Amount, Guid Id);

    public static string Encode(BursaryApplicationListCriteria criteria, BursaryApplication lastOnPage)
    {
        var payload = new Payload(
            criteria.StudentId, criteria.Status, criteria.OrderBy,
            lastOnPage.Deadline.ToUniversalTime().Ticks, lastOnPage.Amount, lastOnPage.Id);
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        return Convert.ToBase64String(json).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static BursaryApplicationCursor Decode(
        string token, Guid? studentId, BursaryApplicationStatus? status, BursaryApplicationSort orderBy)
    {
        Payload? payload;
        try
        {
            var base64 = token.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            payload = JsonSerializer.Deserialize<Payload>(Convert.FromBase64String(base64));
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            payload = null;
        }

        if (payload is null)
            throw new ArgumentException("The page token is malformed.", "pageToken");

        if (payload.StudentId != studentId || payload.Status != status || payload.OrderBy != orderBy)
            throw new ArgumentException(
                "The page token was issued for a different filter or sort. Start again without a page token.",
                "pageToken");

        // Npgsql only accepts UTC DateTimes for "timestamp with time zone".
        return new BursaryApplicationCursor(
            new DateTime(payload.DeadlineTicks, DateTimeKind.Utc), payload.Amount, payload.Id);
    }
}
