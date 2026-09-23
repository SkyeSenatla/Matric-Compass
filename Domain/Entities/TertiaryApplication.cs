namespace Domain.Entities;

// The pipeline from the project brief, in order. Day 1 only needs to read
// this — GET /api/tertiary-applications is read-only for now — so there is
// deliberately no method yet for moving an application through it. That
// arrives once a service layer exists to hold the rule for "who's allowed
// to advance this, and when."
public enum TertiaryApplicationStatus
{
    Researching,
    Applied,
    DocumentsSubmitted,
    Offered,
    Accepted,
    Rejected
}

// An entity, not a DTO — same discipline as Student: it protects its own
// shape at construction time, and nothing outside this class can put it
// into an invalid state.
public class TertiaryApplication : IEntity
{
    public Guid Id { get; }
    public Guid StudentId { get; }
    public string InstitutionName { get; }
    public string ProgrammeName { get; }
    public TertiaryApplicationStatus Status { get; }

    public TertiaryApplication(Guid studentId, string institutionName, string programmeName)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("A tertiary application must belong to a student.", nameof(studentId));
        if (string.IsNullOrWhiteSpace(institutionName))
            throw new ArgumentException("Institution name is required.", nameof(institutionName));
        if (string.IsNullOrWhiteSpace(programmeName))
            throw new ArgumentException("Programme name is required.", nameof(programmeName));

        Id = Guid.NewGuid();
        StudentId = studentId;
        InstitutionName = institutionName;
        ProgrammeName = programmeName;
        Status = TertiaryApplicationStatus.Researching; // every application starts here
    }
}
