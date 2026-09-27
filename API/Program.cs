using Scalar.AspNetCore;
using API.Common;
using API.Data;
using API.Services;
using Domain.Entities;
using FluentValidation;

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

// Day 3: scans this assembly for every class deriving from
// AbstractValidator<T> (BursaryApplicationCreateRequestValidator,
// AptitudeTestCreateRequestValidator, ...) and registers each as
// IValidator<T> — a new validator class needs no registration line here,
// it's found the next time the app starts.
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Day 3: the one catch site for every domain failure in the app. See
// API/Common/DomainExceptionHandler.cs for what it catches and how it
// maps each exception type to a status code.
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails(); // fallback shape for anything the handler above doesn't catch

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

// Day 1: TertiaryApplication needs nothing beyond the generic contract
// yet (it's read-only for now), so it's registered straight against
// IRepository<T> — no entity-specific interface exists for it until a
// rule shows up that the generic shape can't cover.
builder.Services.AddSingleton<IRepository<TertiaryApplication>, InMemoryRepository<TertiaryApplication>>();

// Day 2: BursaryApplication gets the entity-specific interface Student
// already has, for the same reason — GetByStudentIdAsync is the one
// lookup CreateAsync's duplicate-application rule needs beyond generic CRUD.
builder.Services.AddSingleton<IBursaryApplicationRepository, InMemoryBursaryApplicationRepository>();

// Day 3: AptitudeTest has no entity-specific query beyond get-by-id, so it
// closes over the generic IRepository<T>/InMemoryRepository<T> directly —
// no dedicated repository class needed, same reasoning as TertiaryApplication.
builder.Services.AddSingleton<IRepository<AptitudeTest>, InMemoryRepository<AptitudeTest>>();

// Scoped, not Singleton — neither service below holds state of its own
// between requests, so there's no reason to keep one instance alive for
// the app's lifetime.
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IBursaryApplicationService, BursaryApplicationService>();

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

// Day 3: wraps the rest of the pipeline in a try/catch and dispatches any
// escaping exception to DomainExceptionHandler. Placed before
// MapControllers() so it wraps every controller action that follows it —
// this is the only catch site left in the whole app.
app.UseExceptionHandler();

app.MapControllers(); // Activates attribute routing for all [ApiController] classes

// ── Seed data that references OTHER seed data ───────────────────────────
// TertiaryApplication and BursaryApplication both need a real StudentId,
// and Student's seeded ids are only known once InMemoryStudentRepository
// has actually constructed them (they're randomly generated, not fixed).
// So this can't happen inside builder.Services like the repositories
// above — it has to run after the container is built, once, against the
// real registered instances. CreateScope() mirrors how a real request
// would resolve these services, even though nothing here is a request.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var studentRepository = services.GetRequiredService<IStudentRepository>();
    var tertiaryApplicationRepository = services.GetRequiredService<IRepository<TertiaryApplication>>();
    var bursaryApplicationRepository = services.GetRequiredService<IBursaryApplicationRepository>();

    var thandiwe = await studentRepository.GetByLrnAsync("LRN-2026-00114");
    var sipho = await studentRepository.GetByLrnAsync("LRN-2026-00287");

    if (thandiwe is not null && sipho is not null)
    {
        await tertiaryApplicationRepository.AddAsync(
            new TertiaryApplication(thandiwe.Id, "University of Pretoria", "BSc Computer Science"));
        await tertiaryApplicationRepository.AddAsync(
            new TertiaryApplication(sipho.Id, "University of Johannesburg", "BCom Accounting"));

        await bursaryApplicationRepository.AddAsync(
            new BursaryApplication(thandiwe.Id, "NSFAS", 45000m, DateTime.UtcNow.AddMonths(2),
                new[] { "Certified ID Copy", "Proof of Household Income" }));
        await bursaryApplicationRepository.AddAsync(
            new BursaryApplication(sipho.Id, "Funza Lushaka", 60000m, DateTime.UtcNow.AddMonths(1),
                new[] { "Certified ID Copy", "Academic Transcript" }));
    }
}

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

// Exposes the top-level-statement Program class to API.Tests, so
// WebApplicationFactory<Program> can boot this exact app in-process for
// the negative-path tests in Demo 7.
public partial class Program { }
