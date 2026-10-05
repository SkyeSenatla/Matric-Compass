namespace API.Data;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;

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
