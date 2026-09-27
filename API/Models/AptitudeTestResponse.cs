namespace API.Models;

using Domain.Entities;

public record AptitudeTestResponse(
    Guid Id, Guid StudentId, string TestType, DateTime DateTaken, int Score, IReadOnlyCollection<string> RecommendedCareers)
{
    public static AptitudeTestResponse FromEntity(AptitudeTest test) =>
        new(test.Id, test.StudentId, test.TestType, test.DateTaken, test.Score, test.RecommendedCareers);
}
