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
//
// Week 5 Day 3: Version is the token from the GET this client's edit is based
// on. If the row has been written since, the update is rejected with 409
// instead of silently overwriting someone else's change. A missing Version
// binds as 0, which never matches a real xmin — also a 409.
public record BursaryApplicationUpdateRequest(decimal Amount, DateTime Deadline, uint Version);
