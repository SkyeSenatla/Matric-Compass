namespace API.Tests.Services;

using API.Data;
using API.Models;
using API.Services;
using Domain.Exceptions;

// Same pattern as BursaryApplicationServiceTests: the class under test and
// the real InMemoryStudentRepository, no HTTP, no mocking. The repository
// seeds Thandiwe and Sipho on construction (see InMemoryStudentRepository),
// so every test here uses its own freshly generated LRN rather than relying
// on an empty repository.
public class StudentServiceTests
{
    private static StudentService CreateSut(out InMemoryStudentRepository repository)
    {
        repository = new InMemoryStudentRepository();
        return new StudentService(repository);
    }

    [Fact]
    public async Task CreateStudentAsync_throws_ConflictException_for_a_duplicate_learner_reference_number()
    {
        var service = CreateSut(out _);
        var lrn = $"LRN-TEST-{Guid.NewGuid():N}";
        await service.CreateStudentAsync(new StudentCreateRequest("Naledi Khumalo", lrn));

        var request = new StudentCreateRequest("Someone Else", lrn);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateStudentAsync(request));
    }

    [Fact]
    public async Task CreateStudentAsync_allows_a_unique_learner_reference_number()
    {
        var service = CreateSut(out _);
        var request = new StudentCreateRequest("Naledi Khumalo", $"LRN-TEST-{Guid.NewGuid():N}");

        var result = await service.CreateStudentAsync(request);

        Assert.Equal("Naledi Khumalo", result.FullName);
    }
}
