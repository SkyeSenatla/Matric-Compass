namespace Domain.Entities;

// A class, not a record — Student is an ENTITY (it has identity and
// behavior over time), not a value object or a response shape.
//
// Moved into the Domain project on Day 1 of the Requests/Responses module,
// alongside TertiaryApplication — the brief calls for the Api and Domain
// solution structure to exist from here on. Nothing about the class
// changed, only where it lives and that it now implements IEntity so the
// generic repository can work with it.
public class Student : IEntity
{
    private readonly List<Subject> _subjects = new();
    private readonly List<BursaryApplication> _bursaryApplications = new();
    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string LearnerReferenceNumber { get; private set; }
    // Read-only view of internal state — callers cannot bypass EnrollSubject()
    // to mutate the list directly.
    public IReadOnlyCollection<Subject> Subjects => _subjects.AsReadOnly();

    // Week 5 Day 2: there's deliberately no AddBursaryApplication method.
    // BursaryApplication's own repository creates and owns its rows — this
    // collection exists so a query can traverse the relationship
    // (Include(s => s.BursaryApplications)), not so Student can write
    // through it. A navigation you can query is not the same thing as a
    // navigation you're meant to write through.
    public IReadOnlyCollection<BursaryApplication> BursaryApplications => _bursaryApplications.AsReadOnly();

    // For EF Core only: when it loads a row it needs a way to create the
    // object without re-running the guard clauses (or minting a new Id),
    // then fills the properties from the database. Private, so application
    // code still has to go through the validating constructor below.
    private Student()
    {
        FullName = null!;
        LearnerReferenceNumber = null!;
    }

    public Student(string fullName, string learnerReferenceNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(learnerReferenceNumber))
            throw new ArgumentException("Learner reference number is required.", nameof(learnerReferenceNumber));
        Id = Guid.NewGuid();
        FullName = fullName;
        LearnerReferenceNumber = learnerReferenceNumber;
    }

    // A domain method — behavior lives WITH the data it protects.
    public void EnrollSubject(string code, decimal targetMark)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Subject code is required.", nameof(code));
        if (_subjects.Any(s => s.Code == code))
            throw new InvalidOperationException($"Student is already enrolled in {code}.");
        if (_subjects.Count >= 7)
            throw new InvalidOperationException("A student may not be enrolled in more than 7 subjects.");

        _subjects.Add(new Subject(Id, code, targetMark));
    }

    public void UpdateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        FullName = fullName;
    }
}
