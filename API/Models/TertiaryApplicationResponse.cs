namespace API.Models;

using Domain.Entities;

public record TertiaryApplicationResponse(
    Guid Id,
    Guid StudentId,
    string InstitutionName,
    string ProgrammeName,
    string Status)
{
    public static TertiaryApplicationResponse FromEntity(TertiaryApplication application) =>
        new(application.Id, application.StudentId, application.InstitutionName,
            application.ProgrammeName, application.Status.ToString());
}
