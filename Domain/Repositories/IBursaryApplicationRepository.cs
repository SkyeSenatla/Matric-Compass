namespace Domain.Repositories;

using Domain.Entities;

// The one lookup BursaryApplicationService needs beyond generic CRUD:
// every application belonging to a given student, so it can check for a
// duplicate active application with the same funder before creating a
// new one.
public interface IBursaryApplicationRepository : IRepository<BursaryApplication>
{
    Task<IEnumerable<BursaryApplication>> GetByStudentIdAsync(Guid studentId);

    // Week 5 Day 3: one page, filtered and sorted IN THE DATABASE. Returns at
    // most criteria.Take rows — the caller asks for one more than the page
    // size to find out whether another page exists.
    Task<IReadOnlyList<BursaryApplication>> ListAsync(BursaryApplicationListCriteria criteria);

    // Week 5 Day 3: save a tracked application ONLY if it's still at the
    // version the client last saw. Throws DbUpdateConcurrencyException if
    // someone else has written it since.
    Task<bool> UpdateAsync(BursaryApplication application, uint expectedVersion);
}
