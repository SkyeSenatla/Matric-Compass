namespace API.Data;

using Domain.Entities;

// Implements IStudentRepository by inheriting the generic in-memory
// behavior (GetAllAsync, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync)
// from InMemoryRepository<Student>, and adding only the one lookup that's
// specific to Student. This is Week 4's stand-in for a real database — the
// point of coding against IStudentRepository, not this class, is that it
// can be swapped for an EF Core + PostgreSQL implementation in Week 5
// without the controller or service changing at all.
public class InMemoryStudentRepository : InMemoryRepository<Student>, IStudentRepository
{
    public InMemoryStudentRepository() : base(BuildSeedData())
    {
    }

    public async Task<Student?> GetByLrnAsync(string learnerReferenceNumber)
    {
        var students = await GetAllAsync();
        return students.FirstOrDefault(s => s.LearnerReferenceNumber == learnerReferenceNumber);
    }

    // Seed data lives behind the same rules as everything else that creates a
    // Student — even our own repository is not allowed to construct an invalid
    // one or bypass EnrollSubject()'s duplicate/7-subject checks.
    private static List<Student> BuildSeedData()
    {
        var thandiwe = new Student("Thandiwe Nkosi", "LRN-2026-00114");
        thandiwe.EnrollSubject("MATH");
        thandiwe.EnrollSubject("PHSC");
        thandiwe.EnrollSubject("ENGL");

        var sipho = new Student("Sipho Dlamini", "LRN-2026-00287");
        sipho.EnrollSubject("MATL"); // Mathematical Literacy
        sipho.EnrollSubject("LIFE");
        sipho.EnrollSubject("ENGL");
        sipho.EnrollSubject("BSTD"); // Business Studies

        return new List<Student> { thandiwe, sipho };
    }
}
