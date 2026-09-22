namespace API.Data;

using API.Models;

// Implements the IStudentRepository contract using a plain in-memory List<Student>.
//
// This is Week 4's stand-in for a real database. The entire point of coding
// against the IStudentRepository interface (rather than a concrete type) is
// that this class can be deleted and replaced with an EF Core + PostgreSQL
// implementation in Week 5 without the controller changing at all — it only
// ever depends on the interface, never on this class directly.
public class InMemoryStudentRepository : IStudentRepository
{
    // readonly because the LIST REFERENCE itself never changes after construction —
    // we only ever mutate its contents (Add/Remove), never reassign the field.
    private readonly List<Student> _students;

    public InMemoryStudentRepository()
    {
        _students = BuildSeedData();
    }

    public Task<IEnumerable<Student>> GetAllAsync()
    {
        // Task.FromResult wraps an already-known value in a completed Task.
        // Nothing here actually awaits I/O, but every repository method is
        // still shaped as async so the signature never has to change when
        // Week 5 swaps this body for "await _db.Students.ToListAsync()".
        return Task.FromResult(_students.AsEnumerable());
    }

    public Task<Student?> GetByIdAsync(Guid id)
    {
        var student = _students.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(student);
    }

    public Task<Student?> GetByLrnAsync(string learnerReferenceNumber)
    {
        var student = _students.FirstOrDefault(s => s.LearnerReferenceNumber == learnerReferenceNumber);
        return Task.FromResult(student);
    }

    public Task<Student> AddAsync(Student student)
    {
        _students.Add(student);
        return Task.FromResult(student);
    }

    public Task<bool> UpdateAsync(Student student)
    {
        var index = _students.FindIndex(s => s.Id == student.Id);
        if (index == -1)
        {
            // Honest signal to the caller: "there was nothing to update."
            // The controller uses this to decide between 204 and 404.
            return Task.FromResult(false);
        }

        _students[index] = student;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var student = _students.FirstOrDefault(s => s.Id == id);
        if (student is null)
        {
            return Task.FromResult(false);
        }

        _students.Remove(student);
        return Task.FromResult(true);
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
