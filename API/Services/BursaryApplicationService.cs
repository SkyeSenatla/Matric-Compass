namespace API.Services;

using API.Data;
using API.Models;
using Domain.Entities;

// Day 2's service layer, applied to the entity the brief actually asks
// for it on. BursaryApplicationsController talks to
// IBursaryApplicationRepository directly for the pure CRUD actions (get,
// update, delete) — only create has a business rule, so only create goes
// through this class. Same incremental discipline as StudentService.
public class BursaryApplicationService : IBursaryApplicationService
{
    private readonly IBursaryApplicationRepository _bursaryApplicationRepository;

    public BursaryApplicationService(IBursaryApplicationRepository bursaryApplicationRepository)
    {
        _bursaryApplicationRepository = bursaryApplicationRepository;
    }

    public async Task<CreateBursaryApplicationResult> CreateAsync(BursaryApplicationCreateRequest request)
    {
        // A rule about today's date, not about the request's shape — it
        // doesn't belong in BursaryApplication's constructor, which only
        // knows what's true right now, not what "in the past" means.
        if (request.Deadline.Date < DateTime.UtcNow.Date)
            return new CreateBursaryApplicationResult.DeadlineInPast(request.Deadline);

        // Spans a READ across every other application this student has,
        // same shape of rule as the duplicate-LRN check in StudentService.
        var existingForStudent = await _bursaryApplicationRepository.GetByStudentIdAsync(request.StudentId);
        var hasActiveApplication = existingForStudent.Any(a =>
            a.Funder == request.Funder && a.Status != BursaryApplicationStatus.Rejected);

        if (hasActiveApplication)
            return new CreateBursaryApplicationResult.DuplicateActiveApplication(request.Funder);

        var application = new BursaryApplication(
            request.StudentId, request.Funder, request.Amount, request.Deadline, request.RequiredDocuments);

        await _bursaryApplicationRepository.AddAsync(application);

        return new CreateBursaryApplicationResult.Created(BursaryApplicationResponse.FromEntity(application));
    }
}
