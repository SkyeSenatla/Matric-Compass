using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using API.Models;
using API.Data;
using API.Services;
using Domain.Exceptions;

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

    public BursaryApplicationsController(
        IBursaryApplicationRepository bursaryApplicationRepository,
        IBursaryApplicationService bursaryApplicationService)
    {
        _bursaryApplicationRepository = bursaryApplicationRepository;
        _bursaryApplicationService = bursaryApplicationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BursaryApplicationResponse>>> GetAllAsync()
    {
        var applications = await _bursaryApplicationRepository.GetAllAsync();
        return Ok(applications.Select(BursaryApplicationResponse.FromEntity));
        // HTTP 200 OK
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
        var application = await _bursaryApplicationRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Bursary application {id} was not found.");

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

        var application = await _bursaryApplicationService.CreateAsync(request);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = application.Id }, application);
        // HTTP 201 Created + Location
    }

    /// <summary>
    /// Updates the amount and deadline of an existing bursary application.
    /// </summary>
    /// <param name="id">The bursary application's id.</param>
    /// <param name="request">The new amount and deadline.</param>
    /// <response code="204">The application was updated.</response>
    /// <response code="404">No bursary application exists with this id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(Guid id, BursaryApplicationUpdateRequest request)
    {
        // Week 5 Day 2: tracked, because UpdateDetails() has to be noticed by
        // SaveChangesAsync() — see StudentsController.UpdateStudentAsync.
        var application = await _bursaryApplicationRepository.GetByIdAsync(id, trackChanges: true)
            ?? throw new NotFoundException($"Bursary application {id} was not found.");

        application.UpdateDetails(request.Amount, request.Deadline);

        await _bursaryApplicationRepository.UpdateAsync(application);

        return NoContent(); // HTTP 204 No Content
    }

    /// <summary>
    /// Deletes a bursary application.
    /// </summary>
    /// <param name="id">The bursary application's id.</param>
    /// <response code="204">The application was deleted.</response>
    /// <response code="404">No bursary application exists with this id.</response>
    [HttpDelete("{id:guid}")]
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
