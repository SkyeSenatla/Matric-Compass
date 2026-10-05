namespace Domain.Entities;

public class CareerRecommendation : IEntity
{
    public Guid Id { get; private set; }
    public Guid AptitudeTestId { get; private set; }
    public string CareerName { get; private set; }
    public int Rank { get; private set; }

    // For EF Core only — see Subject's private constructor.
    private CareerRecommendation()
    {
        CareerName = null!;
    }

    public CareerRecommendation(Guid aptitudeTestId, string careerName, int rank)
    {
        if (aptitudeTestId == Guid.Empty)
            throw new ArgumentException("A recommendation must belong to an aptitude test.", nameof(aptitudeTestId));
        if (string.IsNullOrWhiteSpace(careerName))
            throw new ArgumentException("Career name is required.", nameof(careerName));
        if (rank < 1)
            throw new ArgumentException("Rank must be 1 or greater.", nameof(rank));

        Id = Guid.NewGuid();
        AptitudeTestId = aptitudeTestId;
        CareerName = careerName;
        Rank = rank;
    }
}
