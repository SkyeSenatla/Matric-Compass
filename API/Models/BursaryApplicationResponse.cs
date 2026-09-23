namespace API.Models;

using Domain.Entities;

public record BursaryApplicationResponse(
    Guid Id,
    Guid StudentId,
    string Funder,
    decimal Amount,
    DateTime Deadline,
    string Status,
    IReadOnlyCollection<string> RequiredDocuments)
{
    public static BursaryApplicationResponse FromEntity(BursaryApplication application) =>
        new(application.Id, application.StudentId, application.Funder, application.Amount,
            application.Deadline, application.Status.ToString(), application.RequiredDocuments);
}
