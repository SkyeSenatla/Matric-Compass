using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using API.Models;
using Domain.Repositories;
using API.Services;
using Domain.Exceptions;
using API.Auth;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers;

// Day 3's full slice: FluentValidation stops a malformed request at the
// door, and every business-rule/not-found failure is a thrown domain
// exception caught exactly once, centrally, by DomainExceptionHandler —
// no try/catch, no switch on a result type, and no hand-written
// NotFound() guard left in this class.
//
// Route spelled out explicitly for the same reason as
// TertiaryApplicationsController: [Route("api/[controller]")] would
// produce "api/BursaryApplications", not the "api/bursary-applications"
// the brief specifies.
[ApiController]
[Route("api/bursary-applications")]
public class BursaryApplicationsController : ControllerBase
{
    private readonly IBursaryApplicationRepository _bursaryApplicationRepository;
    private readonly IBursaryApplicationService _bursaryApplicationService;
    private readonly IAuthorizationService _authorizationService;

    public BursaryApplicationsController(
        IBursaryApplicationRepository bursaryApplicationRepository,
        IBursaryApplicationService bursaryApplicationService,
        IAuthorizationService authorizationService)
    {
        _bursaryApplicationRepository = bursaryApplicationRepository;
        _bursaryApplicationService = bursaryApplicationService;
        _authorizationService = authorizationService;
    }

    // Week 6 Day 1: the same ownership rule as StudentsController, applied to
    // the student who OWNS the application. Same handler, same 404.
    private async Task<bool> CanAccessStudentDataAsync(Guid owningStudentId) =>
        (await _authorizationService.AuthorizeAsync(User, owningStudentId, Policies.StudentDataAccess)).Succeeded;

    // Week 5 Day 3: no longer "return everything". One page at a time,
    // filtered and sorted in the database, behind an opaque page token.
    /// <summary>
    /// Lists bursary applications, one page at a time.
    /// </summary>
    /// <param name="request">Optional pageSize (default 10, max 50), pageToken, studentId, status, and orderBy (deadline or amount).</param>
    /// <response code="200">One page of results. An empty nextPageToken means there are no more.</response>
    /// <response code="400">Negative pageSize, unknown orderBy/status, or a malformed or mismatched pageToken.</response>
    [HttpGet]
    [Authorize(Policy = Policies.StaffOnly)]
    [ProducesResponseType(typeof(PagedResponse<BursaryApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<BursaryApplicationResponse>>> GetAllAsync(
        [FromQuery] BursaryApplicationListRequest request)
    {
        return Ok(await _bursaryApplicationService.ListAsync(request)); // HTTP 200 OK
    }

    /// <summary>
    /// Gets a single bursary application by id.
    /// </summary>
    /// <param name="id">The bursary application's id.</param>
    /// <response code="200">The bursary application was found.</response>
    /// <response code="404">No bursary application exists with this id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BursaryApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BursaryApplicationResponse>> GetByIdAsync(Guid id)
    {
        var application = await _bursaryApplicationRepository.GetByIdAsync(id);

        // Missing and not-yours look identical from the outside — on purpose.
        if (application is null || !await CanAccessStudentDataAsync(application.StudentId))
            throw new NotFoundException($"Bursary application {id} was not found.");

        return Ok(BursaryApplicationResponse.FromEntity(application)); // HTTP 200 OK
    }

    /// <summary>
    /// Creates a new bursary application for a student.
    /// </summary>
    /// <remarks>
    /// Rejects a deadline in the past and a second active application with the
    /// same funder for the same student — see the 409 and 422 responses below.
    /// </remarks>
    /// <param name="request">The funder, amount, deadline and required documents.</param>
    /// <param name="validator">Resolved from DI; validates the request shape.</param>
    /// <response code="201">The application was created.</response>
    /// <response code="400">The request failed validation (see the FluentValidation rules).</response>
    /// <response code="409">This student already has an active application with this funder.</response>
    /// <response code="422">The deadline is in the past.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BursaryApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BursaryApplicationResponse>> CreateAsync(
        BursaryApplicationCreateRequest request,
        IValidator<BursaryApplicationCreateRequest> validator)
    {
        await validator.ValidateAndThrowAsync(request); // throws FluentValidation.ValidationException

        // A learner may only apply on their OWN behalf.
        if (!await CanAccessStudentDataAsync(request.StudentId))
            throw new NotFoundException($"Student {request.StudentId} was not found.");

        var application = await _bursaryApplicationService.CreateAsync(request);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = application.Id }, application);
        // HTTP 201 Created + Location
    }

    /// <summary>
    /// Updates the amount and deadline of an existing bursary application.
    /// </summary>
    /// <param name="id">The bursary application's id.</param>
    /// <param name="request">The new amount and deadline, plus the Version this edit is based on.</param>
    /// <response code="204">The application was updated.</response>
    /// <response code="404">No bursary application exists with this id.</response>
    /// <response code="409">The application changed since the Version you sent was read. GET it again and reapply your change.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(Guid id, BursaryApplicationUpdateRequest request)
    {
        // Week 5 Day 2: tracked, because UpdateDetails() has to be noticed by
        // SaveChangesAsync() — see StudentsController.UpdateStudentAsync.
        var application = await _bursaryApplicationRepository.GetByIdAsync(id, trackChanges: true);
        if (application is null || !await CanAccessStudentDataAsync(application.StudentId))
            throw new NotFoundException($"Bursary application {id} was not found.");

        application.UpdateDetails(request.Amount, request.Deadline);

        // Week 5 Day 3: pass the client's Version through — a stale one makes
        // SaveChanges throw DbUpdateConcurrencyException, which
        // DomainExceptionHandler turns into a 409.
        await _bursaryApplicationRepository.UpdateAsync(application, request.Version);

        return NoContent(); // HTTP 204 No Content
    }

    /// <summary>
    /// Deletes a bursary application.
    /// </summary>
    /// <param name="id">The bursary application's id.</param>
    /// <response code="204">The application was deleted.</response>
    /// <response code="404">No bursary application exists with this id.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.StaffOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        var deleted = await _bursaryApplicationRepository.DeleteAsync(id);

        if (!deleted)
            throw new NotFoundException($"Bursary application {id} was not found.");

        return NoContent(); // HTTP 204 No Content
    }
}
