namespace API.Data;

using Domain.Entities;

public class InMemoryBursaryApplicationRepository : InMemoryRepository<BursaryApplication>, IBursaryApplicationRepository
{
    public async Task<IEnumerable<BursaryApplication>> GetByStudentIdAsync(Guid studentId)
    {
        var applications = await GetAllAsync();
        return applications.Where(a => a.StudentId == studentId);
    }
}
