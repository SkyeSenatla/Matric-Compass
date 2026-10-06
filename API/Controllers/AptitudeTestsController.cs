using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using API.Models;
using Domain.Repositories;
using Domain.Entities;
using Domain.Exceptions;

namespace API.Controllers;

// Day 3's new controller — no service layer at all. There's no
// cross-entity business rule to enforce here, unlike BursaryApplication's
// duplicate-funder check, so this talks straight to the repository, same
// as pure CRUD always has since Day 1. Every failure mode this entity can
// produce — malformed request (ValidateAndThrowAsync), invalid score (the
// constructor's own guard clauses), not found (?? throw NotFoundException)
// — is already handled by DomainExceptionHandler without this file
// needing a single try, catch, or null check of its own.
[ApiController]
[Route("api/aptitude-tests")]
public class AptitudeTestsController : ControllerBase
{
    private readonly IRepository<AptitudeTest> _aptitudeTestRepository;

    public AptitudeTestsController(IRepository<AptitudeTest> aptitudeTestRepository)
    {
        _aptitudeTestRepository = aptitudeTestRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AptitudeTestResponse>>> GetAllAsync()
    {
        var tests = await _aptitudeTestRepository.GetAllAsync();
        return Ok(tests.Select(AptitudeTestResponse.FromEntity)); // HTTP 200 OK
    }

    /// <summary>
    /// Gets a single aptitude test result by id.
    /// </summary>
    /// <param name="id">The aptitude test's id.</param>
    /// <response code="200">The aptitude test was found.</response>
    /// <response code="404">No aptitude test exists with this id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AptitudeTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AptitudeTestResponse>> GetByIdAsync(Guid id)
    {
        var test = await _aptitudeTestRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Aptitude test {id} was not found.");

        return Ok(AptitudeTestResponse.FromEntity(test)); // HTTP 200 OK
    }

    /// <summary>
    /// Records a new aptitude test result for a student and generates career recommendations.
    /// </summary>
    /// <param name="request">The student, test type, date taken and score.</param>
    /// <param name="validator">Resolved from DI; validates the request shape.</param>
    /// <response code="201">The test result was recorded.</response>
    /// <response code="400">The request failed validation (see the FluentValidation rules).</response>
    [HttpPost]
    [ProducesResponseType(typeof(AptitudeTestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AptitudeTestResponse>> CreateAsync(
        AptitudeTestCreateRequest request,
        IValidator<AptitudeTestCreateRequest> validator)
    {
        await validator.ValidateAndThrowAsync(request);

        var test = new AptitudeTest(request.StudentId, request.TestType, request.DateTaken, request.Score);
        await _aptitudeTestRepository.AddAsync(test);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = test.Id }, AptitudeTestResponse.FromEntity(test));
        // HTTP 201 Created + Location
    }
}
