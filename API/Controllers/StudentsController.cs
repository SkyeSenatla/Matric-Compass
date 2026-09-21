using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;

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

    // Constructor injection: ASP.NET Core's DI container sees this controller
    // needs an IStudentRepository, looks in the container configured in
    // Program.cs, finds the registered InMemoryStudentRepository, and hands
    // it in automatically. We never write "new StudentsController(...)"
    // ourselves — the framework does that, once per request.
    public StudentsController(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    // ── GET: /api/students ──────────────────────────────────────────────
    // ActionResult<T> (rather than plain IActionResult) is what lets the
    // built-in OpenAPI generator know the exact shape of a successful
    // response (Student[] here). That typed schema is what Scalar reads to
    // build its "Test Request" UI, and later what the frontend team's
    // generated TypeScript client depends on.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudentsAsync()
    {
        var students = await _studentRepository.GetAllAsync();
        return Ok(students);
        // HTTP 200 OK, body: JSON array of Student objects.
    }

    // ── GET: /api/students/{id} ─────────────────────────────────────────
    // The ":guid" route constraint means ASP.NET Core only matches this
    // route when {id} is a syntactically valid GUID — an invalid format
    // never even reaches this method body. That's free validation.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Student>> GetStudentByIdAsync(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student is null)
        {
            // Guard clause: fail fast, keep the success path unindented below.
            return NotFound(); // HTTP 404 Not Found
        }

        return Ok(student); // HTTP 200 OK
    }

    // ── POST: /api/students ─────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<Student>> CreateStudentAsync(StudentCreateRequest request)
    {
        Student student;

        try
        {
            // Student's constructor is where our invariants actually live —
            // if the request is invalid (blank name, blank LRN), the entity
            // itself throws. The controller's job is only to translate that
            // into an HTTP response, not to duplicate the validation rules.
            student = new Student(request.FullName, request.LearnerReferenceNumber);
        }
        catch (ArgumentException ex)
        {
            // Deliberately manual today: this try/catch has to be repeated in
            // every action that might fail this way. Day 3 replaces this with
            // centralized exception-handling middleware that does it
            // everywhere automatically — you're meant to feel the repetition
            // here so that later refactor makes sense.
            return BadRequest(ex.Message); // HTTP 400 Bad Request
        }

        await _studentRepository.AddAsync(student);

        // CreatedAtAction does three things at once: sets the status code to
        // 201, sets the Location response header to the URL of the new
        // resource (by pointing at GetStudentByIdAsync with the new id), and
        // puts the created object in the response body. That's the complete,
        // correct shape of a create endpoint — not just "return Ok(student)".
        return CreatedAtAction(
            nameof(GetStudentByIdAsync),
            new { id = student.Id },
            student);
        // HTTP 201 Created
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
