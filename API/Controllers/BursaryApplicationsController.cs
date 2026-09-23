using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using API.Services;
using API.Common;

namespace API.Controllers;

// Day 2's full slice: DTOs separated from the entity, a service layer
// holding the one rule that needs it, RFC 9457 Problem Details on every
// business failure, and all four codes from the decision table
// (201+Location, 404, 409, 422).
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BursaryApplicationResponse>> GetByIdAsync(Guid id)
    {
        var application = await _bursaryApplicationRepository.GetByIdAsync(id);

        if (application is null)
            return NotFound(); // HTTP 404 Not Found

        return Ok(BursaryApplicationResponse.FromEntity(application)); // HTTP 200 OK
    }

    [HttpPost]
    public async Task<ActionResult<BursaryApplicationResponse>> CreateAsync(BursaryApplicationCreateRequest request)
    {
        CreateBursaryApplicationResult result;

        try
        {
            // Same discipline as CreateStudentAsync: BursaryApplication's
            // own constructor still throws on genuinely malformed input
            // (blank funder, non-positive amount), and we still catch that
            // here, by hand, action by action.
            result = await _bursaryApplicationService.CreateAsync(request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message); // HTTP 400 Bad Request
        }

        return result switch
        {
            CreateBursaryApplicationResult.Created created =>
                CreatedAtAction(nameof(GetByIdAsync), new { id = created.Application.Id }, created.Application),
                // HTTP 201 Created + Location

            CreateBursaryApplicationResult.DuplicateActiveApplication duplicate =>
                ProblemResponses.Conflict(
                    $"This student already has an active bursary application with {duplicate.Funder}.",
                    "/api/bursary-applications"),
                // HTTP 409 Conflict

            CreateBursaryApplicationResult.DeadlineInPast deadline =>
                ProblemResponses.UnprocessableEntity(
                    $"Deadline {deadline.Deadline:yyyy-MM-dd} is in the past.",
                    "/api/bursary-applications"),
                // HTTP 422 Unprocessable Entity

            _ => throw new InvalidOperationException("Unhandled CreateBursaryApplicationResult case.")
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, BursaryApplicationUpdateRequest request)
    {
        var application = await _bursaryApplicationRepository.GetByIdAsync(id);

        if (application is null)
            return NotFound(); // HTTP 404 Not Found — nothing to update

        try
        {
            application.UpdateDetails(request.Amount, request.Deadline);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message); // HTTP 400 Bad Request
        }

        await _bursaryApplicationRepository.UpdateAsync(application);

        return NoContent(); // HTTP 204 No Content
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        var deleted = await _bursaryApplicationRepository.DeleteAsync(id);

        if (!deleted)
            return NotFound(); // HTTP 404 Not Found

        return NoContent(); // HTTP 204 No Content
    }
}
