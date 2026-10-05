namespace Domain.Entities;

// "One Guardian, one linked Student, to start" — enforced in
// MatricCompassDbContext with a real unique index, not just a comment. The
// day a guardian needs a second linked student, THIS constraint is what
// has to come out, deliberately, not by accident of someone forgetting it
// was there.
public class Guardian : IEntity
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public string FullName { get; private set; }
    public string ContactNumber { get; private set; }

    // For EF Core only — see Subject's private constructor.
    private Guardian()
    {
        FullName = null!;
        ContactNumber = null!;
    }

    public Guardian(Guid studentId, string fullName, string contactNumber)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("A guardian must be linked to a student.", nameof(studentId));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(contactNumber))
            throw new ArgumentException("Contact number is required.", nameof(contactNumber));

        Id = Guid.NewGuid();
        StudentId = studentId;
        FullName = fullName;
        ContactNumber = contactNumber;
    }
}
