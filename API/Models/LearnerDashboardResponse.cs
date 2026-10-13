namespace API.Models;

// Week 6 Day 3: shaped for ONE screen — the learner's dashboard — not for
// one table. Everything the screen shows and nothing it doesn't: no LRN, no
// StudentId, no concurrency Version. The browser makes one request and
// renders; it never joins, filters or calculates.
public record LearnerDashboardResponse(
    string FullName,
    IReadOnlyList<DashboardSubject> Subjects,
    IReadOnlyList<DashboardBursaryApplication> BursaryApplications);

// OnTrack is decided here, once — not re-implemented in every client.
public record DashboardSubject(string Code, decimal CurrentMark, decimal TargetMark, bool OnTrack);

// AmountInCents: a JavaScript number is a binary double. Whole cents are
// exact; R45 000.10 as a double is not. Status is the enum's NAME, never its
// number. Deadline is UTC (the "Z" on the end); the browser converts it to
// local time. DaysUntilDeadline uses the server's clock, not the phone's.
public record DashboardBursaryApplication(
    Guid Id,
    string Funder,
    string Status,
    DateTime Deadline,
    int DaysUntilDeadline,
    long AmountInCents);
