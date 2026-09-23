namespace API.Data;

using Domain.Entities;

// The one lookup BursaryApplicationService needs beyond generic CRUD:
// every application belonging to a given student, so it can check for a
// duplicate active application with the same funder before creating a
// new one.
public interface IBursaryApplicationRepository : IRepository<BursaryApplication>
{
    Task<IEnumerable<BursaryApplication>> GetByStudentIdAsync(Guid studentId);
}
