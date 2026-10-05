namespace API.Models;

// Raw query-string input for GET /api/bursary-applications. Everything is
// optional and everything is a string or nullable on purpose — turning it
// into validated criteria (and rejecting nonsense with a 400) is the
// service's job, not model binding's.
public class BursaryApplicationListRequest
{
    public int? PageSize { get; init; }
    public string? PageToken { get; init; }
    public Guid? StudentId { get; init; }
    public string? Status { get; init; }
    public string? OrderBy { get; init; }
}
