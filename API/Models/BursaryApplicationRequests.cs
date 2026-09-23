namespace API.Models;

// Used by POST /api/bursary-applications to create a new application.
public record BursaryApplicationCreateRequest(
    Guid StudentId,
    string Funder,
    decimal Amount,
    DateTime Deadline,
    List<string> RequiredDocuments);

// Used by PUT /api/bursary-applications/{id} to update amount and deadline.
// Funder and Student are fixed at creation — changing either is a new
// application, not an edit of this one.
public record BursaryApplicationUpdateRequest(decimal Amount, DateTime Deadline);
