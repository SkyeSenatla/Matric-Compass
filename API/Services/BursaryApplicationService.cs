namespace API.Services;

using API.Common;
using Domain.Repositories;
using API.Models;
using Domain.Entities;
using Domain.Exceptions;

// Day 2's service layer, applied to the entity the brief actually asks
// for it on. BursaryApplicationsController talks to
// IBursaryApplicationRepository directly for the pure CRUD actions (get,
// update, delete) — only create has a business rule, so only create goes
// through this class. Same incremental discipline as StudentService.
//
// Day 3: throws a domain exception instead of returning a result the
// caller has to switch on — DomainExceptionHandler is guaranteed to be
// there to catch it, so the "it failed" case no longer needs to be
// represented in this method's return type at all.
public class BursaryApplicationService : IBursaryApplicationService
{
    private readonly IBursaryApplicationRepository _bursaryApplicationRepository;

    public BursaryApplicationService(IBursaryApplicationRepository bursaryApplicationRepository)
    {
        _bursaryApplicationRepository = bursaryApplicationRepository;
    }

    public async Task<BursaryApplicationResponse> CreateAsync(BursaryApplicationCreateRequest request)
    {
        // A rule about today's date, not about the request's shape — it
        // doesn't belong in BursaryApplication's constructor, which only
        // knows what's true right now, not what "in the past" means.
        if (request.Deadline.Date < DateTime.UtcNow.Date)
            throw new UnprocessableEntityException($"Deadline {request.Deadline:yyyy-MM-dd} is in the past.");

        // Spans a READ across every other application this student has,
        // same shape of rule as the duplicate-LRN check in StudentService.
        var existingForStudent = await _bursaryApplicationRepository.GetByStudentIdAsync(request.StudentId);
        var hasActiveApplication = existingForStudent.Any(a =>
            a.Funder == request.Funder && a.Status != BursaryApplicationStatus.Rejected);

        if (hasActiveApplication)
            throw new ConflictException($"This student already has an active bursary application with {request.Funder}.");

        var application = new BursaryApplication(
            request.StudentId, request.Funder, request.Amount, request.Deadline, request.RequiredDocuments);

        await _bursaryApplicationRepository.AddAsync(application);

        return BursaryApplicationResponse.FromEntity(application);
    }

    // Week 5 Day 3: the paging contract (AIP-158), enforced in ONE place.
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    public async Task<PagedResponse<BursaryApplicationResponse>> ListAsync(BursaryApplicationListRequest request)
    {
        // Page size: optional; 0 or missing means "server decides"; too big is
        // quietly reduced to the maximum, not rejected; negative is a 400.
        if (request.PageSize is < 0)
            throw new ArgumentException("pageSize must not be negative.", nameof(request.PageSize));
        var pageSize = request.PageSize is null or 0 ? DefaultPageSize : Math.Min(request.PageSize.Value, MaxPageSize);

        // Sort and filter come from an allow-list — anything else is a 400,
        // never an arbitrary ORDER BY the database has no index for.
        var orderBy = BursaryApplicationSort.Deadline;
        if (request.OrderBy is not null
            && !Enum.TryParse(request.OrderBy, ignoreCase: true, out orderBy))
            throw new ArgumentException(
                $"orderBy must be one of: {string.Join(", ", Enum.GetNames<BursaryApplicationSort>())}.",
                nameof(request.OrderBy));

        BursaryApplicationStatus? status = null;
        if (request.Status is not null)
        {
            if (!Enum.TryParse<BursaryApplicationStatus>(request.Status, ignoreCase: true, out var parsed))
                throw new ArgumentException(
                    $"status must be one of: {string.Join(", ", Enum.GetNames<BursaryApplicationStatus>())}.",
                    nameof(request.Status));
            status = parsed;
        }

        var after = string.IsNullOrEmpty(request.PageToken)
            ? null
            : PageToken.Decode(request.PageToken, request.StudentId, status, orderBy);

        // Ask for ONE more row than the page holds: if it comes back, there's
        // a next page — no COUNT(*) needed to find that out.
        var criteria = new BursaryApplicationListCriteria(request.StudentId, status, orderBy, after, pageSize + 1);
        var rows = await _bursaryApplicationRepository.ListAsync(criteria);

        var page = rows.Take(pageSize).ToList();
        var nextPageToken = rows.Count > pageSize ? PageToken.Encode(criteria, page[^1]) : "";

        return new PagedResponse<BursaryApplicationResponse>(
            page.Select(BursaryApplicationResponse.FromEntity).ToList(), nextPageToken);
    }
}
