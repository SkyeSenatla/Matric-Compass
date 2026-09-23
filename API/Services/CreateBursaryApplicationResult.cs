namespace API.Services;

using API.Models;

// Same shape of thinking as CreateStudentResult: an application CREATED,
// or a specific, expected reason it wasn't. Both failure cases here need
// context beyond one entity's own fields — a duplicate check reads every
// OTHER application for this student, and "is this deadline still in the
// future" is a business rule about today's date, not about this request's
// shape. Neither belongs in BursaryApplication's constructor.
public abstract record CreateBursaryApplicationResult
{
    public sealed record Created(BursaryApplicationResponse Application) : CreateBursaryApplicationResult;
    public sealed record DuplicateActiveApplication(string Funder) : CreateBursaryApplicationResult;
    public sealed record DeadlineInPast(DateTime Deadline) : CreateBursaryApplicationResult;
}
