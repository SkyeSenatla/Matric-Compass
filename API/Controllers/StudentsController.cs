using Microsoft.AspNetCore.Mvc;
using API.Models;
using Domain.Repositories;
using API.Services;
using Domain.Exceptions;
using Domain.Entities;
using API.Auth;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers;

// [ApiController] activates several automatic behaviours: model binding from
// JSON request bodies, automatic 400 responses for malformed/invalid models,
// and problem-details-shaped error formatting. It marks this class as an API
// controller (not an MVC page controller).
//
// [Route("api/[controller]")] sets the base URL for every action in this
// class. The [controller] token is replaced with the class name minus the
// "Controller" suffix: StudentsController -> api/students. This is automatic
// and consistent across every controller we add later.
[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentService _studentService;
    private readonly IAuthorizationService _authorizationService;

    // Constructor injection: ASP.NET Core's DI container sees this controller
    // needs an IStudentRepository, looks in the container configured in
    // Program.cs, finds the registered InMemoryStudentRepository, and hands
    // it in automatically. We never write "new StudentsController(...)"
    // ourselves — the framework does that, once per request.
    //
    // IStudentService is new today. We didn't remove IStudentRepository —
    // GetStudentsAsync, GetStudentByIdAsync, UpdateStudentAsync and
    // DeleteStudentAsync are still pure CRUD with no decision to move, so
    // they stay on the repository directly. Only CreateStudentAsync and the
    // new payments endpoint have a business rule, so only they go through
    // the service.
    public StudentsController(
        IStudentRepository studentRepository,
        IStudentService studentService,
        IAuthorizationService authorizationService)
    {
        _studentRepository = studentRepository;
        _studentService = studentService;
        _authorizationService = authorizationService;
    }

    // Week 6 Day 1: resource-based authorization. [Authorize] can't do this
    // check — it runs BEFORE the action, before we know which student the
    // request is about. So the action asks, once it knows.
    //
    // Not yours? 404, not 403. A 403 would confirm the student exists; a 404
    // tells a curious learner nothing about other learners (OWASP: IDOR).
    private async Task EnsureCanAccessStudentAsync(Guid studentId)
    {
        var result = await _authorizationService.AuthorizeAsync(User, studentId, Policies.StudentDataAccess);
        if (!result.Succeeded)
            throw new NotFoundException($"Student {studentId} was not found.");
    }

    // ── GET: /api/students ──────────────────────────────────────────────
    // ActionResult<T> (rather than plain IActionResult) is what lets the
    // built-in OpenAPI generator know the exact shape of a successful
    // response (Student[] here). That typed schema is what Scalar reads to
    // build its "Test Request" UI, and later what the frontend team's
    // generated TypeScript client depends on.
    //
    // Week 6 Day 1: policy-based. Every learner's record in one response is
    // for staff only.
    [HttpGet]
    [Authorize(Policy = Policies.StaffOnly)]
    [ProducesResponseType(typeof(IEnumerable<StudentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<StudentResponse>>> GetStudentsAsync()
    {
        var students = await _studentRepository.GetAllAsync();
        return Ok(students.Select(StudentResponse.FromEntity));
        // HTTP 200 OK, body: JSON array of StudentResponse objects — the
        // Student entity itself never crosses the HTTP boundary (Day 2).
    }

    // ── GET: /api/students/{id} ─────────────────────────────────────────
    // The ":guid" route constraint means ASP.NET Core only matches this
    // route when {id} is a syntactically valid GUID — an invalid format
    // never even reaches this method body. That's free validation.
    /// <summary>
    /// Gets a single student by id.
    /// </summary>
    /// <param name="id">The student's id.</param>
    /// <response code="200">The student was found.</response>
    /// <response code="404">No student exists with this id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StudentResponse>> GetStudentByIdAsync(Guid id)
    {
        await EnsureCanAccessStudentAsync(id);

        var student = await _studentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Student {id} was not found.");

        return Ok(StudentResponse.FromEntity(student)); // HTTP 200 OK
    }

    // ── POST: /api/students ─────────────────────────────────────────────
    // Day 2: this is the one CRUD action that gained a real business rule
    // (no two students share an LRN), and a real business rule is exactly
    // what earns a class in the service layer. CreateStudentAsync now
    // delegates entirely — it decides nothing about students, only about
    // HTTP.
    //
    // Day 3: no try/catch left here. StudentService throws ConflictException
    // on a duplicate LRN and Student's own constructor throws
    // ArgumentException on malformed input — both land in
    // DomainExceptionHandler without this action needing to know either
    // exists.
    /// <summary>
    /// Creates a new student record.
    /// </summary>
    /// <remarks>
    /// Rejects a learner reference number that already belongs to another
    /// student — see the 409 response below.
    /// </remarks>
    /// <param name="request">The student's full name and learner reference number.</param>
    /// <response code="201">The student was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">A student with this learner reference number already exists.</response>
    [HttpPost]
    [Authorize(Policy = Policies.StaffOnly)]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentResponse>> CreateStudentAsync(StudentCreateRequest request)
    {
        var student = await _studentService.CreateStudentAsync(request);

        // CreatedAtAction does three things at once: sets the status code to
        // 201, sets the Location response header to the URL of the new
        // resource, and puts the created object in the response body.
        return CreatedAtAction(nameof(GetStudentByIdAsync), new { id = student.Id }, student);
        // HTTP 201 Created
    }

    // ── PUT: /api/students/{id} ─────────────────────────────────────────
    /// <summary>
    /// Updates a student's full name.
    /// </summary>
    /// <param name="id">The student's id.</param>
    /// <param name="request">The new full name.</param>
    /// <response code="204">The student was updated.</response>
    /// <response code="404">No student exists with this id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStudentAsync(Guid id, StudentUpdateRequest request)
    {
        await EnsureCanAccessStudentAsync(id);

        // Week 5 Day 2: one of only two call sites in the app that need a
        // tracked entity — UpdateAsync relies on the change tracker to
        // notice FullName changed. Every read-only caller takes the default.
        var student = await _studentRepository.GetByIdAsync(id, trackChanges: true)
            ?? throw new NotFoundException($"Student {id} was not found.");

        // Same discipline as creation: the entity validates itself via
        // UpdateFullName(). We deliberately do NOT do
        // "student.FullName = request.FullName" here — the setter is
        // private, so the compiler won't even let us. That's the rich
        // domain model holding up under a write operation, not just a read.
        // A malformed name throws ArgumentException, caught centrally by
        // DomainExceptionHandler — nothing local catches it anymore.
        student.UpdateFullName(request.FullName);

        await _studentRepository.UpdateAsync(student);

        // 204, not 200 with a body: the client already knows what it sent
        // and doesn't need the full object echoed back. Both 200 and 204 are
        // legitimate choices for a successful update — the important thing
        // is picking one and staying consistent across the whole API.
        return NoContent(); // HTTP 204 No Content
    }

    // ── DELETE: /api/students/{id} ──────────────────────────────────────
    /// <summary>
    /// Deletes a student.
    /// </summary>
    /// <param name="id">The student's id.</param>
    /// <response code="204">The student was deleted.</response>
    /// <response code="404">No student exists with this id.</response>
    //
    // Week 6 Day 1: role-based, for contrast with the policies above. It
    // works — but the rule "only Admin deletes" now lives in this attribute,
    // and every other place that rule applies has to repeat it.
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStudentAsync(Guid id)
    {
        // DeleteAsync returning bool means we don't need a separate fetch
        // just to check existence first — one repository call gives us
        // everything needed to decide between 204 and NotFoundException.
        var deleted = await _studentRepository.DeleteAsync(id);

        if (!deleted)
            throw new NotFoundException($"Student {id} was not found.");

        return NoContent(); // HTTP 204 No Content
        // Calling DELETE again on the same id now throws NotFoundException
        // instead of 204 — the response differs, but the end state (student
        // is gone) is identical either way. That's what makes DELETE
        // idempotent.
    }
}
