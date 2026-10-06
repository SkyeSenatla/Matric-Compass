namespace Infrastructure.Data;

using Domain.Entities;
using Domain.Repositories;

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

    // Same contract as EfBursaryApplicationRepository.ListAsync, in LINQ to
    // Objects — filtering in memory is fine here, because memory is where
    // this repository's data already lives.
    public async Task<IReadOnlyList<BursaryApplication>> ListAsync(BursaryApplicationListCriteria criteria)
    {
        var query = (await GetAllAsync()).AsQueryable();

        if (criteria.StudentId is { } studentId)
            query = query.Where(b => b.StudentId == studentId);
        if (criteria.Status is { } status)
            query = query.Where(b => b.Status == status);

        var after = criteria.After;
        query = criteria.OrderBy switch
        {
            BursaryApplicationSort.Amount => (after is null ? query : query.Where(b =>
                    b.Amount > after.Amount || (b.Amount == after.Amount && b.Id.CompareTo(after.Id) > 0)))
                .OrderBy(b => b.Amount).ThenBy(b => b.Id),
            _ => (after is null ? query : query.Where(b =>
                    b.Deadline > after.Deadline || (b.Deadline == after.Deadline && b.Id.CompareTo(after.Id) > 0)))
                .OrderBy(b => b.Deadline).ThenBy(b => b.Id)
        };

        return query.Take(criteria.Take).ToList();
    }

    // Day 2's leaky abstraction again: there's no xmin in a Dictionary, so
    // there's nothing to compare expectedVersion against. Accepted and
    // ignored, on purpose, named as such.
    public Task<bool> UpdateAsync(BursaryApplication application, uint expectedVersion) =>
        UpdateAsync(application);
}
