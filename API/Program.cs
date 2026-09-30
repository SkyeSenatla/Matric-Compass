using Scalar.AspNetCore;
using API.Common;
using API.Data;
using API.Services;
using Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

// ════════════════════════════════════════════════════
// PHASE 1 — BUILDER: Register services into the
// Dependency Injection container
// ════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

// Week 5: a connection string has a password in it — it lives in user
// secrets (`dotnet user-secrets set ...`, see the API project's
// UserSecretsId), never in appsettings.json, never committed. Failing
// loudly here, at startup, with a message that names the exact command to
// fix it, beats a silent null connection string failing confusingly three
// layers deeper the first time something tries to query.
var connectionString = builder.Configuration.GetConnectionString("MatricCompass")
    ?? throw new InvalidOperationException(
        "Connection string 'MatricCompass' was not found. Run 'dotnet user-secrets set " +
        "ConnectionStrings:MatricCompass \"...\"' inside the API project.");

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

// Week 5: connection pooling is on by default in the connection string
// above — nothing to add for that. EnableRetryOnFailure() is not
// automatic, and it's the first time this app has had to think about a
// network call that can fail for reasons that have nothing to do with the
// request itself (a dropped connection, a Postgres restart mid-query).
builder.Services.AddDbContext<MatricCompassDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));

// Register the repository abstraction: any controller that asks for
// IStudentRepository in its constructor receives this instance automatically.
// We never call "new EfStudentRepository()" anywhere else in the app.
//
// Week 5: Scoped, not Singleton — this is the lifetime flip Day 1's slide
// deck warned about. A DbContext must not survive past one request, and
// EfStudentRepository holds one, so nothing that depends on it can be
// Singleton anymore without recreating that exact bug. Students now live in
// the real database (see MatricCompassDbContext); TertiaryApplication,
// BursaryApplication, and AptitudeTest stay in-memory a while longer.
builder.Services.AddScoped<IStudentRepository, EfStudentRepository>();

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
// and Student's seeded ids are only known once the student rows actually
// exist (they're randomly generated, not fixed). So this can't happen
// inside builder.Services like the repositories above — it has to run
// after the container is built, once, against the real registered
// instances. CreateScope() mirrors how a real request would resolve these
// services, even though nothing here is a request.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Week 5: applies any pending migrations on startup, so a fresh clone
    // of this repo just needs a running Postgres and a connection string —
    // not a manual "remember to run dotnet ef database update" step.
    var dbContext = services.GetRequiredService<MatricCompassDbContext>();
    await dbContext.Database.MigrateAsync();

    // InMemoryStudentRepository seeded Thandiwe and Sipho in its
    // constructor — a real database doesn't construct itself with rows, so
    // seeding now has to be an explicit, idempotent step: only insert if
    // they're not already there, so restarting the app never duplicates them.
    if (!await dbContext.Students.AnyAsync())
    {
        var thandiweSeed = new Student("Thandiwe Nkosi", "LRN-2026-00114");
        thandiweSeed.EnrollSubject("MATH");
        thandiweSeed.EnrollSubject("PHSC");
        thandiweSeed.EnrollSubject("ENGL");

        var siphoSeed = new Student("Sipho Dlamini", "LRN-2026-00287");
        siphoSeed.EnrollSubject("MATL");
        siphoSeed.EnrollSubject("LIFE");
        siphoSeed.EnrollSubject("ENGL");
        siphoSeed.EnrollSubject("BSTD");

        dbContext.Students.AddRange(thandiweSeed, siphoSeed);
        await dbContext.SaveChangesAsync();
    }

    var studentRepository = services.GetRequiredService<IStudentRepository>();
    var tertiaryApplicationRepository = services.GetRequiredService<IRepository<TertiaryApplication>>();
    var bursaryApplicationRepository = services.GetRequiredService<IBursaryApplicationRepository>();

    var thandiwe = await studentRepository.GetByLrnAsync("LRN-2026-00114");
    var sipho = await studentRepository.GetByLrnAsync("LRN-2026-00287");

    if (thandiwe is not null && sipho is not null
        && !(await tertiaryApplicationRepository.GetAllAsync()).Any())
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
