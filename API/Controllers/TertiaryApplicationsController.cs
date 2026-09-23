using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Domain.Entities;

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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TertiaryApplicationResponse>> GetByIdAsync(Guid id)
    {
        var application = await _tertiaryApplicationRepository.GetByIdAsync(id);

        if (application is null)
            return NotFound(); // HTTP 404 Not Found

        return Ok(TertiaryApplicationResponse.FromEntity(application)); // HTTP 200 OK
    }
}
