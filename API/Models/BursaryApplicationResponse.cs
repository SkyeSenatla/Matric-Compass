namespace API.Models;

using API.Common;
using Domain.Entities;

public record BursaryApplicationResponse(
    Guid Id,
    Guid StudentId,
    string Funder,
    // Week 7 Day 3: was "decimal Amount" — a JSON number with a fraction,
    // which breaks the handoff rule that money is integer cents.
    long AmountInCents,
    DateTime Deadline,
    string Status,
    IReadOnlyCollection<string> RequiredDocuments,
    // Week 5 Day 3: the concurrency token goes OUT with every read, so the
    // client can send it back with its update.
    uint Version)
{
    public static BursaryApplicationResponse FromEntity(BursaryApplication application) =>
        new(application.Id, application.StudentId, application.Funder, Money.ToCents(application.Amount),
            application.Deadline, application.Status.ToString(), application.RequiredDocuments,
            application.Version);
}
