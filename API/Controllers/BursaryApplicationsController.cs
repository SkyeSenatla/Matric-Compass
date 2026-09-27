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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BursaryApplicationResponse>> GetByIdAsync(Guid id)
    {
        var application = await _bursaryApplicationRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Bursary application {id} was not found.");

        return Ok(BursaryApplicationResponse.FromEntity(application)); // HTTP 200 OK
    }

    [HttpPost]
    public async Task<ActionResult<BursaryApplicationResponse>> CreateAsync(
        BursaryApplicationCreateRequest request,
        IValidator<BursaryApplicationCreateRequest> validator)
    {
        await validator.ValidateAndThrowAsync(request); // throws FluentValidation.ValidationException

        var application = await _bursaryApplicationService.CreateAsync(request);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = application.Id }, application);
        // HTTP 201 Created + Location
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, BursaryApplicationUpdateRequest request)
    {
        var application = await _bursaryApplicationRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Bursary application {id} was not found.");

        application.UpdateDetails(request.Amount, request.Deadline);

        await _bursaryApplicationRepository.UpdateAsync(application);

        return NoContent(); // HTTP 204 No Content
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        var deleted = await _bursaryApplicationRepository.DeleteAsync(id);

        if (!deleted)
            throw new NotFoundException($"Bursary application {id} was not found.");

        return NoContent(); // HTTP 204 No Content
    }
}
