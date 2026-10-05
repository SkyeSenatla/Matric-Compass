namespace API.Models;

using Domain.Entities;

public record AptitudeTestResponse(
    Guid Id, Guid StudentId, string TestType, DateTime DateTaken, int Score, IReadOnlyCollection<string> RecommendedCareers)
{
    // Week 5 Day 2: the domain model got richer (CareerRecommendation with a
    // Rank) but the wire contract didn't have to move — still a plain list
    // of career names, in rank order.
    public static AptitudeTestResponse FromEntity(AptitudeTest test) =>
        new(test.Id, test.StudentId, test.TestType, test.DateTaken, test.Score,
            test.Recommendations.OrderBy(r => r.Rank).Select(r => r.CareerName).ToList());
}
