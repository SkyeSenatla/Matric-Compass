namespace API.Data;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;

// The real implementation the comment in InMemoryStudentRepository has
// been promising since Day 1. IStudentRepository, StudentService, and
// StudentsController do not change — that's the entire point.
public class EfStudentRepository : IStudentRepository
{
    private readonly MatricCompassDbContext _dbContext;

    public EfStudentRepository(MatricCompassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Eager loading: the shape of what's needed is decided here, in the
    // query, before anything runs — one round trip, not one per student.
    public async Task<IEnumerable<Student>> GetAllAsync() =>
        await _dbContext.Students.AsNoTracking()
            .Include(s => s.Subjects)
            .Include(s => s.BursaryApplications)
            .ToListAsync();

    public async Task<Student?> GetByIdAsync(Guid id, bool trackChanges = false)
    {
        var query = trackChanges
            ? _dbContext.Students.Include(s => s.Subjects).Include(s => s.BursaryApplications)
            : _dbContext.Students.AsNoTracking().Include(s => s.Subjects).Include(s => s.BursaryApplications);
        return await query.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Student?> GetByLrnAsync(string learnerReferenceNumber) =>
        await _dbContext.Students.FirstOrDefaultAsync(s => s.LearnerReferenceNumber == learnerReferenceNumber);

    public async Task<Student> AddAsync(Student student)
    {
        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync();
        return student;
    }

    public async Task<bool> UpdateAsync(Student student)
    {
        // The instance passed in was loaded from THIS same DbContext
        // earlier in the same request (every controller action does
        // GetByIdAsync first) — it's already tracked. Update() would be
        // wrong here: it would tell EF Core to treat every property as
        // changed, not just FullName, which is harmless today but a real
        // bug the moment a second concurrent write only touches one field.
        var exists = await _dbContext.Students.AnyAsync(s => s.Id == student.Id);
        if (!exists)
            return false;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.Id == id);
        if (student is null)
            return false;

        _dbContext.Students.Remove(student);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
