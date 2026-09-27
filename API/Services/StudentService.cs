namespace API.Services;

using API.Data;
using API.Models;
using Domain.Entities;
using Domain.Exceptions;

// Day 3: throws ConflictException instead of returning a result the
// caller has to switch on — DomainExceptionHandler is guaranteed to be
// there to catch it. Student's own constructor still owns "is this a
// well-formed student at all"; that's a different kind of failure
// (ArgumentException), and DomainExceptionHandler maps that to 400 too.
public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<StudentResponse> CreateStudentAsync(StudentCreateRequest request)
    {
        // The rule the controller had no good place for: it spans a READ
        // and a decision, not just the validation of one entity.
        var existing = await _studentRepository.GetByLrnAsync(request.LearnerReferenceNumber);
        if (existing is not null)
            throw new ConflictException($"A student with LRN {request.LearnerReferenceNumber} already exists.");

        var student = new Student(request.FullName, request.LearnerReferenceNumber);
        await _studentRepository.AddAsync(student);

        return StudentResponse.FromEntity(student);
    }
}
