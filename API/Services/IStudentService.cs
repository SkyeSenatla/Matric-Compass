namespace API.Services;

using API.Models;

public interface IStudentService
{
    Task<CreateStudentResult> CreateStudentAsync(StudentCreateRequest request);
}
