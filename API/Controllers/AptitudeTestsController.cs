using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using API.Models;
using API.Data;
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AptitudeTestResponse>> GetByIdAsync(Guid id)
    {
        var test = await _aptitudeTestRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Aptitude test {id} was not found.");

        return Ok(AptitudeTestResponse.FromEntity(test)); // HTTP 200 OK
    }

    [HttpPost]
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
