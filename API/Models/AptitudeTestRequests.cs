namespace API.Models;

// Used by POST /api/aptitude-tests to record a new test result.
public record AptitudeTestCreateRequest(Guid StudentId, string TestType, DateTime DateTaken, int Score);
