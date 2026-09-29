using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Domain.Entities;
using Domain.Exceptions;

namespace API.Controllers;

// Day 1: read-only for now. No IRepository<TertiaryApplication>-specific
// interface exists yet because nothing beyond GetAllAsync/GetByIdAsync is
// needed — the generic contract is enough on its own. That changes the
// day this controller needs to write, same as Student did.
//
// The route is spelled out explicitly (kebab-case, plural) rather than
// relying on [Route("api/[controller]")], which would produce
// "api/TertiaryApplications" instead of the "api/tertiary-applications"
// the project brief specifies.
[ApiController]
[Route("api/tertiary-applications")]
public class TertiaryApplicationsController : ControllerBase
{
    private readonly IRepository<TertiaryApplication> _tertiaryApplicationRepository;

    public TertiaryApplicationsController(IRepository<TertiaryApplication> tertiaryApplicationRepository)
    {
        _tertiaryApplicationRepository = tertiaryApplicationRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TertiaryApplicationResponse>>> GetAllAsync()
    {
        var applications = await _tertiaryApplicationRepository.GetAllAsync();
        return Ok(applications.Select(TertiaryApplicationResponse.FromEntity));
        // HTTP 200 OK
    }

    /// <summary>
    /// Gets a single tertiary application by id.
    /// </summary>
    /// <param name="id">The tertiary application's id.</param>
    /// <response code="200">The tertiary application was found.</response>
    /// <response code="404">No tertiary application exists with this id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TertiaryApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TertiaryApplicationResponse>> GetByIdAsync(Guid id)
    {
        var application = await _tertiaryApplicationRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tertiary application {id} was not found.");

        return Ok(TertiaryApplicationResponse.FromEntity(application)); // HTTP 200 OK
    }
}
