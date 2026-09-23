namespace API.Services;

using API.Data;
using API.Models;
using Domain.Entities;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<CreateStudentResult> CreateStudentAsync(StudentCreateRequest request)
    {
        // The rule the controller had no good place for: it spans a READ
        // and a decision, not just the validation of one entity.
        var existing = await _studentRepository.GetByLrnAsync(request.LearnerReferenceNumber);
        if (existing is not null)
            return new CreateStudentResult.DuplicateLearnerReferenceNumber(request.LearnerReferenceNumber);

        var student = new Student(request.FullName, request.LearnerReferenceNumber);
        await _studentRepository.AddAsync(student);

        return new CreateStudentResult.Created(StudentResponse.FromEntity(student));
    }
}
