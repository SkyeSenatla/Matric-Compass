namespace API.Tests.Services;

using API.Data;
using API.Models;
using API.Services;
using Domain.Exceptions;

// No HTTP, no DI container, no WebApplicationFactory — just the class under
// test and the same InMemoryBursaryApplicationRepository the app itself
// uses. We're not mocking IBursaryApplicationRepository with something like
// Moq: the in-memory repository already IS a fast test double, so a mock
// here would just be ceremony for a seam that already exists. That trade
// reverses once Week 5 swaps in a real EF Core repository.
public class BursaryApplicationServiceTests
{
    private static BursaryApplicationService CreateSut(out InMemoryBursaryApplicationRepository repository)
    {
        repository = new InMemoryBursaryApplicationRepository();
        return new BursaryApplicationService(repository);
    }

    [Fact]
    public async Task CreateAsync_throws_UnprocessableEntityException_when_deadline_is_in_the_past()
    {
        var service = CreateSut(out _);
        var request = new BursaryApplicationCreateRequest(
            Guid.NewGuid(), "NSFAS", 1000m, DateTime.UtcNow.AddDays(-1), new List<string>());

        await Assert.ThrowsAsync<UnprocessableEntityException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_throws_ConflictException_for_a_second_active_application_with_the_same_funder()
    {
        var service = CreateSut(out var repository);
        var studentId = Guid.NewGuid();
        await repository.AddAsync(new Domain.Entities.BursaryApplication(
            studentId, "NSFAS", 5000m, DateTime.UtcNow.AddMonths(1), new List<string>()));

        var request = new BursaryApplicationCreateRequest(
            studentId, "NSFAS", 1000m, DateTime.UtcNow.AddMonths(2), new List<string>());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_allows_a_second_application_with_a_different_funder()
    {
        var service = CreateSut(out var repository);
        var studentId = Guid.NewGuid();
        await repository.AddAsync(new Domain.Entities.BursaryApplication(
            studentId, "NSFAS", 5000m, DateTime.UtcNow.AddMonths(1), new List<string>()));

        var request = new BursaryApplicationCreateRequest(
            studentId, "Funza Lushaka", 1000m, DateTime.UtcNow.AddMonths(2), new List<string>());
        var result = await service.CreateAsync(request);

        Assert.Equal("Funza Lushaka", result.Funder);
    }
}
