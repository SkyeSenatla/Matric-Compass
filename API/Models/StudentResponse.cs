namespace API.Models;

// The response DTO Day 1 deliberately deferred — see the comment in
// StudentRequests.cs. A record, same discipline as StudentCreateRequest
// and StudentUpdateRequest: flat, immutable, no behavior. FromEntity is
// the one and only place that decides what a Student looks like once it
// crosses the HTTP boundary.
public record StudentResponse(
    Guid Id,
    string FullName,
    string LearnerReferenceNumber,
    IReadOnlyCollection<string> SubjectCodes)
{
    public static StudentResponse FromEntity(Student student) =>
        new(student.Id, student.FullName, student.LearnerReferenceNumber, student.SubjectCodes);
}
