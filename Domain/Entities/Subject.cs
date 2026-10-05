namespace Domain.Entities;

// The entity yesterday's Ignore() call was standing in for.
public class Subject : IEntity
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public string Code { get; private set; }
    public decimal CurrentMark { get; private set; }
    public decimal TargetMark { get; private set; }

    // For EF Core only — no validation, no random Id. EF Core fills every
    // property in via reflection once this returns. Same reason Student,
    // BursaryApplication, and AptitudeTest all needed one on Day 1.
    private Subject()
    {
        Code = null!;
    }

    public Subject(Guid studentId, string code, decimal targetMark)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("A subject must belong to a student.", nameof(studentId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Subject code is required.", nameof(code));
        if (targetMark is < 0 or > 100)
            throw new ArgumentException("Target mark must be between 0 and 100.", nameof(targetMark));

        Id = Guid.NewGuid();
        StudentId = studentId;
        Code = code;
        TargetMark = targetMark;
        CurrentMark = 0;
    }

    public void RecordMark(decimal currentMark)
    {
        if (currentMark is < 0 or > 100)
            throw new ArgumentException("Current mark must be between 0 and 100.", nameof(currentMark));

        CurrentMark = currentMark;
    }
}
