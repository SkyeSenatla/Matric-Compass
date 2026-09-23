namespace API.Data;

using Domain.Entities;

// Student needs one lookup the generic contract doesn't offer. That's
// exactly what "IRepository<T> introduced FOR Student" means in practice:
// the generic interface covers the CRUD every entity shares, and a
// specific interface adds only what this one entity actually needs on
// top of it — GetByLrnAsync, for the duplicate-LRN rule in StudentService.
public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByLrnAsync(string learnerReferenceNumber);
}
