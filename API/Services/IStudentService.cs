namespace API.Services;

using API.Models;

public interface IStudentService
{
    Task<StudentResponse> CreateStudentAsync(StudentCreateRequest request);
}
