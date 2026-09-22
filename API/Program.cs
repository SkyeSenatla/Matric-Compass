using Scalar.AspNetCore;
using API.Data;
using API.Services;

// ════════════════════════════════════════════════════
// PHASE 1 — BUILDER: Register services into the
// Dependency Injection container
// ════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

// Since .NET 8, ASP.NET Core trims the "Async" suffix off action names by
// default when it builds the route table (MvcOptions.SuppressAsyncSuffixInActionNames
// defaults to true). That silently breaks CreatedAtAction(nameof(GetStudentByIdAsync), ...)
// in the controller below: nameof() gives the literal C# method name
// ("GetStudentByIdAsync"), but the registered route name would be
// "GetStudentById" — a mismatch that throws "No route matches the supplied
// values" at runtime, not at compile time. We keep full method names as
// route names so nameof() stays a reliable, refactor-safe way to reference
// an action.
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
}); // Register controller support
builder.Services.AddOpenApi();         // Register built-in OpenAPI document generation

// Register the repository abstraction: any controller that asks for
// IStudentRepository in its constructor receives this instance automatically.
// We never call "new InMemoryStudentRepository()" anywhere else in the app.
//
// Why AddSingleton? Our "database" is just a List<Student> living in process
// memory. If this were Scoped or Transient, a brand-new empty list would be
// created on every request (or every scope), and our data would vanish
// between calls. Singleton is correct TODAY ONLY — once Week 5 introduces a
// real database, Scoped becomes the right lifetime, because the database
// itself is the shared state, not an in-memory object we're keeping alive
// artificially.
builder.Services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();

// Singleton for the same reason as IStudentRepository above: this needs
// to survive between requests, and today there's exactly one process
// holding it.
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

// Scoped, not Singleton — unlike the two registrations above,
// StudentService holds no state of its own between requests, so there's
// no reason to keep one instance alive for the app's lifetime.
builder.Services.AddScoped<IStudentService, StudentService>();

// ════════════════════════════════════════════════════
// TRANSITION — Build() seals the DI container.
// Nothing can be registered after this line.
// ════════════════════════════════════════════════════
var app = builder.Build();

// ════════════════════════════════════════════════════
// PHASE 2 — PIPELINE: Configure the middleware chain.
// Order matters. Every request passes through these
// in sequence, top to bottom.
// ════════════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // Serves /openapi/v1.json
    app.MapScalarApiReference();   // Serves the Scalar UI at /scalar/v1
}

app.MapControllers(); // Activates attribute routing for all [ApiController] classes

// ── For reference only: this is what the same GET endpoint would look like
// as a Minimal API instead of a controller action (see StudentsController).
// Both approaches are valid ASP.NET Core; we chose controllers for this
// project because Matric Compass will grow into many grouped endpoints
// (Students, Guardians, TertiaryApplications, BursaryApplications,
// AptitudeTests) and controllers organise that better than a flat list of
// app.MapGet/MapPost calls in Program.cs.
//
// app.MapGet("/api/students", async (IStudentRepository repo) =>
// {
//     var students = await repo.GetAllAsync();
//     return Results.Ok(students);
// });

app.Run(); // Starts the Kestrel web server — this line blocks until the process exits
