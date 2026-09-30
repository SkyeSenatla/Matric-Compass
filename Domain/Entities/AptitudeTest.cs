namespace Domain.Entities;

// Day 3's new entity, cheap because the pattern already exists: an
// immutable entity with get-only properties, guard clauses in the
// constructor throwing plain ArgumentException, and no dedicated exception
// type of its own — "malformed input" is the only failure mode it has, and
// DomainExceptionHandler already maps ArgumentException to 400.
public class AptitudeTest : IEntity
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public string TestType { get; private set; }
    public DateTime DateTaken { get; private set; }
    public int Score { get; private set; }
    public IReadOnlyCollection<string> RecommendedCareers { get; private set; }

    // For EF Core only — see Student's private constructor.
    private AptitudeTest()
    {
        TestType = null!;
        RecommendedCareers = null!;
    }

    public AptitudeTest(Guid studentId, string testType, DateTime dateTaken, int score)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("An aptitude test must belong to a student.", nameof(studentId));
        if (string.IsNullOrWhiteSpace(testType))
            throw new ArgumentException("Test type is required.", nameof(testType));
        if (score is < 0 or > 100)
            throw new ArgumentException("Score must be between 0 and 100.", nameof(score));

        Id = Guid.NewGuid();
        StudentId = studentId;
        TestType = testType;
        DateTaken = dateTaken;
        Score = score;
        RecommendedCareers = GenerateRecommendations(score);
    }

    // Real behavior, generated at construction — not bolted on somewhere
    // else. A seeded, rules-based mapping is explicitly all the brief asks
    // for here.
    private static IReadOnlyCollection<string> GenerateRecommendations(int score) => score switch
    {
        >= 85 => new[] { "Actuarial Science", "Electrical Engineering", "Medicine" },
        >= 70 => new[] { "Computer Science", "Accounting", "Architecture" },
        >= 50 => new[] { "Education", "Nursing", "Business Administration" },
        _ => new[] { "Technical and Vocational Education (TVET)" }
    };
}
