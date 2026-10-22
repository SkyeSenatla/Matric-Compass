namespace API.Models;

// Used by POST /api/bursary-applications to create a new application.
//
// Week 7 Day 4: money arrives as integer cents, like every response sends
// it. The browser turns the rands a person typed into cents; the API never
// receives a fraction of a rand as a JSON number.
public record BursaryApplicationCreateRequest(
    Guid StudentId,
    string Funder,
    long AmountInCents,
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
public record BursaryApplicationUpdateRequest(long AmountInCents, DateTime Deadline, uint Version);
