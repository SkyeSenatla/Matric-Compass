namespace API.Data;

using Domain.Entities;

// Week 5 Day 2: the app no longer registers this (EfBursaryApplicationRepository
// replaced it), but it stays — BursaryApplicationServiceTests uses it as a
// fast, database-free test double for IBursaryApplicationRepository.
public class InMemoryBursaryApplicationRepository : InMemoryRepository<BursaryApplication>, IBursaryApplicationRepository
{
    public async Task<IEnumerable<BursaryApplication>> GetByStudentIdAsync(Guid studentId)
    {
        var applications = await GetAllAsync();
        return applications.Where(a => a.StudentId == studentId);
    }
}
