namespace Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;

// Week 5 Day 2: Day 1's EfStudentRepository swap, repeated for
// BursaryApplication — the N+1 demo needs "students with their bursary
// applications" to be one real query against one real database.
public class EfBursaryApplicationRepository : IBursaryApplicationRepository
{
    private readonly MatricCompassDbContext _dbContext;

    public EfBursaryApplicationRepository(MatricCompassDbContext dbContext) => _dbContext = dbContext;

    public async Task<IEnumerable<BursaryApplication>> GetAllAsync() =>
        await _dbContext.BursaryApplications.AsNoTracking().ToListAsync();

    public async Task<BursaryApplication?> GetByIdAsync(Guid id, bool trackChanges = false)
    {
        var query = trackChanges ? _dbContext.BursaryApplications : _dbContext.BursaryApplications.AsNoTracking();
        return await query.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<BursaryApplication>> GetByStudentIdAsync(Guid studentId) =>
        await _dbContext.BursaryApplications.AsNoTracking().Where(b => b.StudentId == studentId).ToListAsync();

    // Week 5 Day 3: every Where, OrderBy and Take below is composed onto an
    // IQueryable BEFORE ToListAsync() — so all of it becomes SQL (WHERE,
    // ORDER BY, LIMIT), and only one page of rows ever leaves Postgres.
    //
    // Keyset ("seek") paging: instead of OFFSET n (read n rows, throw them
    // away), resume strictly AFTER the last row the client saw, using the
    // sort column plus Id as a unique tiebreaker.
    //
    // The "b.Deadline >= after.Deadline &&" part is logically redundant —
    // the OR after it already implies it. It's there for Postgres: an OR
    // can't be used as an index range condition, so without it the planner
    // walks the index from the student's FIRST application and discards
    // every row before the cursor (measured: 40,001 rows thrown away on a
    // deep page). With it, ">=" becomes the index's starting point.
    public async Task<IReadOnlyList<BursaryApplication>> ListAsync(BursaryApplicationListCriteria criteria)
    {
        var query = _dbContext.BursaryApplications.AsNoTracking();

        if (criteria.StudentId is { } studentId)
            query = query.Where(b => b.StudentId == studentId);
        if (criteria.Status is { } status)
            query = query.Where(b => b.Status == status);

        var after = criteria.After;
        query = criteria.OrderBy switch
        {
            BursaryApplicationSort.Amount => (after is null ? query : query.Where(b =>
                    b.Amount >= after.Amount
                    && (b.Amount > after.Amount || b.Id.CompareTo(after.Id) > 0)))
                .OrderBy(b => b.Amount).ThenBy(b => b.Id),
            _ => (after is null ? query : query.Where(b =>
                    b.Deadline >= after.Deadline
                    && (b.Deadline > after.Deadline || b.Id.CompareTo(after.Id) > 0)))
                .OrderBy(b => b.Deadline).ThenBy(b => b.Id)
        };

        return await query.Take(criteria.Take).ToListAsync();
    }

    public async Task<BursaryApplication> AddAsync(BursaryApplication application)
    {
        _dbContext.BursaryApplications.Add(application);
        await _dbContext.SaveChangesAsync();
        return application;
    }

    public async Task<bool> UpdateAsync(BursaryApplication application)
    {
        // Same reasoning as EfStudentRepository.UpdateAsync: the instance was
        // loaded with trackChanges: true earlier in this request, so
        // SaveChangesAsync() only writes the properties that actually changed.
        var exists = await _dbContext.BursaryApplications.AnyAsync(b => b.Id == application.Id);
        if (!exists)
            return false;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(BursaryApplication application, uint expectedVersion)
    {
        var exists = await _dbContext.BursaryApplications.AnyAsync(b => b.Id == application.Id);
        if (!exists)
            return false;

        // The entity was loaded (tracked) moments ago, so its Version is the
        // CURRENT xmin — checking against that would only catch a write in
        // the last few milliseconds. The client's edit is based on the
        // version from ITS earlier GET, so that's the value EF Core must put
        // in "WHERE xmin = @original".
        _dbContext.Entry(application).Property(b => b.Version).OriginalValue = expectedVersion;

        await _dbContext.SaveChangesAsync(); // 0 rows → DbUpdateConcurrencyException
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var application = await _dbContext.BursaryApplications.FirstOrDefaultAsync(b => b.Id == id);
        if (application is null)
            return false;

        _dbContext.BursaryApplications.Remove(application);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
