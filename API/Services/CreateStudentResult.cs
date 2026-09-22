namespace API.Services;

using API.Models;

// A student CREATED, or a specific, expected reason it wasn't — never an
// exception thrown across the service boundary for an outcome the caller
// should have to handle anyway. Student's own constructor still owns
// "is this a well-formed student at all"; that's a different kind of
// failure, and it still throws, same as Day 1.
public abstract record CreateStudentResult
{
    public sealed record Created(StudentResponse Student) : CreateStudentResult;
    public sealed record DuplicateLearnerReferenceNumber(string LearnerReferenceNumber) : CreateStudentResult;
}
