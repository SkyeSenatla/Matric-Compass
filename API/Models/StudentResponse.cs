namespace API.Models;

using Domain.Entities;

// The response DTO Day 1 deliberately deferred — see the comment in
// StudentRequests.cs. A record, same discipline as StudentCreateRequest
// and StudentUpdateRequest: flat, immutable, no behavior. FromEntity is
// the one and only place that decides what a Student looks like once it
// crosses the HTTP boundary.
public record StudentResponse(
    Guid Id,
    string FullName,
    string LearnerReferenceNumber,
    IReadOnlyCollection<SubjectResponse> Subjects)
{
    // Week 5 Day 2: maps each Subject to its own DTO rather than returning
    // the entity — Subject never crosses the HTTP boundary either.
    public static StudentResponse FromEntity(Student student) =>
        new(student.Id, student.FullName, student.LearnerReferenceNumber,
            student.Subjects.Select(s => new SubjectResponse(s.Code, s.CurrentMark, s.TargetMark)).ToList());
}

public record SubjectResponse(string Code, decimal CurrentMark, decimal TargetMark);
