namespace Domain.Entities;

// Week 6 Day 1: someone who can sign in. Deliberately NOT the same thing as
// a Student: a counsellor signs in but isn't a student, and a student record
// can exist before (or without) anyone ever having a login for it.
//
// The domain stores a password HASH and nothing else. It never sees the
// password itself, and it doesn't know which hashing algorithm made the
// hash — that's the API's job (ASP.NET Core's PasswordHasher).
public class AppUser : IEntity
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; }

    // Only a Learner is linked to a Student record. This link is what
    // resource-ownership checks compare against: "is this YOUR student?"
    public Guid? StudentId { get; private set; }

    // For EF Core only — see Student's private constructor.
    private AppUser()
    {
        Email = null!;
        PasswordHash = null!;
        Role = null!;
    }

    public AppUser(string email, string role, Guid? studentId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (!Roles.All.Contains(role))
            throw new ArgumentException($"Unknown role '{role}'.", nameof(role));
        if (role == Roles.Learner && studentId is null)
            throw new ArgumentException("A learner account must be linked to a student.", nameof(studentId));
        if (role != Roles.Learner && studentId is not null)
            throw new ArgumentException("Only a learner account can be linked to a student.", nameof(studentId));

        Id = Guid.NewGuid();
        Email = NormalizeEmail(email);
        Role = role;
        StudentId = studentId;
        PasswordHash = string.Empty;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    // "Thandiwe@Example.com " and "thandiwe@example.com" are the same person.
    // Normalising in ONE place means the unique index and the login lookup
    // can never disagree.
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
