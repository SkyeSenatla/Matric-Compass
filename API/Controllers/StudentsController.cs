using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using API.Services;
using API.Common;

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
    public StudentsController(IStudentRepository studentRepository, IStudentService studentService)
    {
        _studentRepository = studentRepository;
        _studentService = studentService;
    }

    // ── GET: /api/students ──────────────────────────────────────────────
    // ActionResult<T> (rather than plain IActionResult) is what lets the
    // built-in OpenAPI generator know the exact shape of a successful
    // response (Student[] here). That typed schema is what Scalar reads to
    // build its "Test Request" UI, and later what the frontend team's
    // generated TypeScript client depends on.
    [HttpGet]
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
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentResponse>> GetStudentByIdAsync(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student is null)
        {
            // Guard clause: fail fast, keep the success path unindented below.
            return NotFound(); // HTTP 404 Not Found
        }

        return Ok(StudentResponse.FromEntity(student)); // HTTP 200 OK
    }

    // ── POST: /api/students ─────────────────────────────────────────────
    // Day 2: this is the one CRUD action that gained a real business rule
    // (no two students share an LRN), and a real business rule is exactly
    // what earns a class in the service layer. CreateStudentAsync now
    // delegates entirely — it decides nothing about students, only about
    // HTTP.
    [HttpPost]
    public async Task<ActionResult<StudentResponse>> CreateStudentAsync(StudentCreateRequest request)
    {
        CreateStudentResult result;

        try
        {
            // Same discipline as Day 1: Student's constructor still throws on
            // genuinely invalid input, and we still catch that here, by hand,
            // action by action. Day 3 is where that stops being repeated.
            result = await _studentService.CreateStudentAsync(request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message); // HTTP 400 Bad Request
        }

        // CreatedAtAction does three things at once: sets the status code to
        // 201, sets the Location response header to the URL of the new
        // resource, and puts the created object in the response body.
        return result switch
        {
            CreateStudentResult.Created created =>
                CreatedAtAction(nameof(GetStudentByIdAsync), new { id = created.Student.Id }, created.Student),
                // HTTP 201 Created

            CreateStudentResult.DuplicateLearnerReferenceNumber duplicate =>
                ProblemResponses.Conflict(
                    $"A student with LRN {duplicate.LearnerReferenceNumber} already exists.",
                    "/api/students"),
                // HTTP 409 Conflict, RFC 9457 shape

            _ => throw new InvalidOperationException("Unhandled CreateStudentResult case.")
        };
    }

    // ── PUT: /api/students/{id} ─────────────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStudentAsync(Guid id, StudentUpdateRequest request)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student is null)
        {
            return NotFound(); // HTTP 404 Not Found — nothing to update
        }

        try
        {
            // Same discipline as creation: the entity validates itself via
            // UpdateFullName(). We deliberately do NOT do
            // "student.FullName = request.FullName" here — the setter is
            // private, so the compiler won't even let us. That's the rich
            // domain model holding up under a write operation, not just a read.
            student.UpdateFullName(request.FullName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message); // HTTP 400 Bad Request
        }

        await _studentRepository.UpdateAsync(student);

        // 204, not 200 with a body: the client already knows what it sent
        // and doesn't need the full object echoed back. Both 200 and 204 are
        // legitimate choices for a successful update — the important thing
        // is picking one and staying consistent across the whole API.
        return NoContent(); // HTTP 204 No Content
    }

    // ── DELETE: /api/students/{id} ──────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStudentAsync(Guid id)
    {
        // DeleteAsync returning bool means we don't need a separate fetch
        // just to check existence first — one repository call gives us
        // everything needed to decide between 204 and 404.
        var deleted = await _studentRepository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(); // HTTP 404 Not Found
        }

        return NoContent(); // HTTP 204 No Content
        // Calling DELETE again on the same id now returns 404 instead of 204
        // — the response differs, but the end state (student is gone) is
        // identical either way. That's what makes DELETE idempotent.
    }
}
