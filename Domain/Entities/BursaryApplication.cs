namespace Domain.Entities;

public enum BursaryApplicationStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected
}

// Day 2's new entity. Same discipline as Student and TertiaryApplication:
// the constructor only enforces what's true about ONE application in
// isolation (a funder name, a positive amount). Rules that need to look at
// OTHER applications — no duplicate active application with the same
// funder, no deadline in the past — need context this entity doesn't have,
// which is exactly why they live in BursaryApplicationService instead.
public class BursaryApplication : IEntity
{
    private readonly List<string> _requiredDocuments;

    public Guid Id { get; }
    public Guid StudentId { get; }
    public string Funder { get; }
    public decimal Amount { get; private set; }
    public DateTime Deadline { get; private set; }
    public BursaryApplicationStatus Status { get; }
    public IReadOnlyCollection<string> RequiredDocuments => _requiredDocuments.AsReadOnly();

    public BursaryApplication(
        Guid studentId, string funder, decimal amount, DateTime deadline, IEnumerable<string> requiredDocuments)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("A bursary application must belong to a student.", nameof(studentId));
        if (string.IsNullOrWhiteSpace(funder))
            throw new ArgumentException("Funder is required.", nameof(funder));
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        Id = Guid.NewGuid();
        StudentId = studentId;
        Funder = funder;
        Amount = amount;
        Deadline = deadline;
        Status = BursaryApplicationStatus.Draft;
        _requiredDocuments = requiredDocuments?.ToList() ?? new List<string>();
    }

    // Same discipline as Student.UpdateFullName — updating goes through
    // validation too, not just a property set.
    public void UpdateDetails(decimal amount, DateTime deadline)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        Amount = amount;
        Deadline = deadline;
    }
}
