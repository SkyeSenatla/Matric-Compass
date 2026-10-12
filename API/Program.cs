using Scalar.AspNetCore;
using API.Common;
using API.Services;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text;
using API.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using API.Observability;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
// Week 6 Day 1: tells the OpenAPI document (and so Scalar) that this API
// expects "Authorization: Bearer <token>" — Scalar then shows a box to paste
// the accessToken from POST /api/auth/login into.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the accessToken returned by POST /api/auth/login."
        };
        document.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }
        ];
        return Task.CompletedTask;
    });
});

// ── Week 6 Day 1: authentication — "who are you?" ───────────────────────
// Issuer/Audience/lifetimes come from appsettings.json; SigningKey from
// user-secrets. Same fail-loudly rule as the connection string: a missing or
// short key is a startup error with the fix in the message, not a 500 on
// the first login.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
    throw new InvalidOperationException(
        "Jwt:SigningKey is missing or shorter than 32 bytes. Run 'dotnet user-secrets set " +
        "Jwt:SigningKey \"<at least 32 random characters>\"' inside the API project.");
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep "sub" and "role" as "sub" and "role". Without this, ASP.NET
        // Core renames incoming claims to long legacy URIs
        // (http://schemas.xmlsoap.org/...) and FindFirst("role") finds nothing.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            // Only the algorithm we sign with. Never trust the token's own
            // "alg" header to pick (OWASP JWT cheat sheet: "alg": "none").
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            // The default is 5 minutes — which would quietly turn a
            // 5-minute access token into a 10-minute one.
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = AppClaims.Role
        };
    });

// ── Week 6 Day 1: authorization — "what may you do?" ────────────────────
builder.Services.AddAuthorizationBuilder()
    // Policy-based: controllers name the policy, this line owns the rule.
    .AddPolicy(Policies.StaffOnly, policy => policy.RequireRole(Roles.Counsellor, Roles.Admin))
    // Resource-based: needs to know WHICH student — see StudentDataAccessHandler.
    .AddPolicy(Policies.StudentDataAccess, policy => policy.AddRequirements(new StudentDataAccessRequirement()))
    // Secure by default: any endpoint without its own [Authorize] or
    // [AllowAnonymous] still requires a signed-in user. Forgetting an
    // attribute now fails CLOSED (401), not open.
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Singletons: none of these hold per-request state.
builder.Services.AddSingleton<IAuthorizationHandler, StudentDataAccessHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

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
builder.Services.AddProblemDetails(options =>
{
    // Week 6 Day 2: problem responses the FRAMEWORK writes (401, 403, model
    // binding 400s) get the same correlationId as the ones our handler
    // writes, so every error a client can see carries the trace id.
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] = Correlation.GetId(context.HttpContext);
}); // fallback shape for anything the handler above doesn't catch

// Week 5: connection pooling is on by default in the connection string
// above — nothing to add for that. EnableRetryOnFailure() is not
// automatic, and it's the first time this app has had to think about a
// network call that can fail for reasons that have nothing to do with the
// request itself (a dropped connection, a Postgres restart mid-query).
//
// Week 6 Day 2: the Week 5 LogTo(Console.WriteLine) line is gone. EF Core
// already logs every SQL command through ILogger (category
// Microsoft.EntityFrameworkCore.Database.Command), so LogTo printed each one
// TWICE. Now one setting in appsettings controls it: Information in
// Development, Warning in production. Parameter VALUES are never logged
// (they show as '?') unless EnableSensitiveDataLogging() is switched on —
// never do that outside a laptop.
builder.Services.AddDbContext<MatricCompassDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null));
});

// Register the repository abstraction: any controller that asks for
// IStudentRepository in its constructor receives this instance automatically.
// We never call "new EfStudentRepository()" anywhere else in the app.
//
// Week 5: Scoped, not Singleton — this is the lifetime flip Day 1's slide
// deck warned about. A DbContext must not survive past one request, and
// EfStudentRepository holds one, so nothing that depends on it can be
// Singleton anymore without recreating that exact bug. Students now live in
// the real database (see MatricCompassDbContext); TertiaryApplication and
// AptitudeTest stay in-memory a while longer.
builder.Services.AddScoped<IStudentRepository, EfStudentRepository>();

// Day 1: TertiaryApplication needs nothing beyond the generic contract
// yet (it's read-only for now), so it's registered straight against
// IRepository<T> — no entity-specific interface exists for it until a
// rule shows up that the generic shape can't cover.
builder.Services.AddSingleton<IRepository<TertiaryApplication>, InMemoryRepository<TertiaryApplication>>();

// Day 2: BursaryApplication gets the entity-specific interface Student
// already has, for the same reason — GetByStudentIdAsync is the one
// lookup CreateAsync's duplicate-application rule needs beyond generic CRUD.
//
// Week 5 Day 2: swapped to EF Core, Scoped for the same reason as
// EfStudentRepository above.
// Was: builder.Services.AddSingleton<IBursaryApplicationRepository, InMemoryBursaryApplicationRepository>();
builder.Services.AddScoped<IBursaryApplicationRepository, EfBursaryApplicationRepository>();

// Day 3: AptitudeTest has no entity-specific query beyond get-by-id, so it
// closes over the generic IRepository<T>/InMemoryRepository<T> directly —
// no dedicated repository class needed, same reasoning as TertiaryApplication.
builder.Services.AddSingleton<IRepository<AptitudeTest>, InMemoryRepository<AptitudeTest>>();

// Scoped, not Singleton — neither service below holds state of its own
// between requests, so there's no reason to keep one instance alive for
// the app's lifetime.

builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IBursaryApplicationService, BursaryApplicationService>();

// Week 6 Day 1: Scoped — both repositories hold a DbContext, and AuthService
// depends on them, so it can't outlive a request either.
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
builder.Services.AddScoped<AuthService>();

// ── Week 6 Day 2: observability — logs, traces and metrics ──────────────
// OpenTelemetry collects what .NET already produces (ILogger, Activity,
// Meter) and sends it wherever we point it. The service name is how this
// API is labelled in the dashboard.
builder.Services.AddSingleton<MatricCompassMetrics>();

var openTelemetry = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("matric-compass-api"))
    .WithTracing(tracing => tracing
        // One span per incoming request. Health probes are skipped: they
        // arrive every few seconds and would bury the real traffic.
        .AddAspNetCoreInstrumentation(options =>
            options.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health"))
        // One child span per SQL command, with the SQL text (never the
        // parameter VALUES) — this is the N+1 waterfall from Week 5 Day 2.
        .AddNpgsql())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()               // request rate, errors, duration
        .AddMeter("System.Runtime")                   // GC, thread pool, CPU (built into .NET)
        .AddMeter(MatricCompassMetrics.MeterName))    // our own sign-in counters
    .WithLogging();

// Only export when an OTLP endpoint is configured (launchSettings.json sets
// it for `dotnet run`). Tests and CI don't set it, so they never try to
// reach a dashboard that isn't there.
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
    openTelemetry.UseOtlpExporter();

// ── Week 6 Day 2: health checks ─────────────────────────────────────────
// Tagged "ready": only the readiness endpoint runs it. Liveness must never
// depend on the database (see the slides: one DB blip, every container
// restarted at once).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MatricCompassDbContext>("database", tags: ["ready"]);

// A probe has to answer FAST. EnableRetryOnFailure (Week 5) retries the
// health check's connection attempt too — with Postgres down, readiness
// took ~20 seconds to say "Unhealthy", and Kubernetes gives a probe 1
// second by default. Cap every check at 3 seconds: a slow answer IS an
// unhealthy answer.
builder.Services.Configure<HealthCheckServiceOptions>(options =>
{
    foreach (var registration in options.Registrations)
        registration.Timeout = TimeSpan.FromSeconds(3);
});

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
    // Week 6 Day 1: the fallback policy covers EVERY endpoint, including
    // these two — without AllowAnonymous the docs themselves need a token.
    app.MapOpenApi().AllowAnonymous();              // Serves /openapi/v1.json
    app.MapScalarApiReference().AllowAnonymous();   // Serves the Scalar UI at /scalar/v1
}

// Day 3: wraps the rest of the pipeline in a try/catch and dispatches any
// escaping exception to DomainExceptionHandler. Placed before
// MapControllers() so it wraps every controller action that follows it —
// this is the only catch site left in the whole app.
app.UseExceptionHandler();

// Week 6 Day 1: a 401 or 403 from the auth middleware has NO body by
// default. With AddProblemDetails() registered, this turns every empty
// error response into application/problem+json — one failure shape, still.
app.UseStatusCodePages();

// Order matters: first work out WHO the caller is, then decide what they
// may do. Both must sit before MapControllers.
app.UseAuthentication();
app.UseAuthorization();

// Week 6 Day 2: liveness runs NO checks — if the process can answer at
// all, it's alive. Readiness runs the "ready"-tagged checks (Postgres).
// Both are anonymous: the platform probing them has no token, and the
// response is just "Healthy" or "Unhealthy" — nothing about our internals.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
    .AllowAnonymous();

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
        thandiweSeed.EnrollSubject("MATH", 80);
        thandiweSeed.EnrollSubject("PHSC", 90);
        thandiweSeed.EnrollSubject("ENGL", 81);

        var siphoSeed = new Student("Sipho Dlamini", "LRN-2026-00287");
        siphoSeed.EnrollSubject("MATL", 76);
        siphoSeed.EnrollSubject("LIFE", 56);
        siphoSeed.EnrollSubject("ENGL", 88);
        siphoSeed.EnrollSubject("BSTD", 32);

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
    }
    // Week 5 Day 2: guarded independently of the tertiary seed above.
    // TertiaryApplication is still in-memory (empty on every restart, so its
    // check is always meaningful), but BursaryApplication now persists —
    // reusing Tertiary's empty-check here would duplicate every bursary
    // application on every restart.
    //
    // Week 5 Day 3: was "!(await bursaryApplicationRepository.GetAllAsync()).Any()"
    // — which downloaded EVERY bursary application into memory just to ask
    // "is there at least one?". AnyAsync() asks Postgres instead, and gets
    // back a single boolean (SELECT EXISTS ...).
    
    if (thandiwe is not null && sipho is not null
        && !await dbContext.BursaryApplications.AnyAsync())
    {
        await bursaryApplicationRepository.AddAsync(
            new BursaryApplication(thandiwe.Id, "NSFAS", 45000m, DateTime.UtcNow.AddMonths(2),
                new[] { "Certified ID Copy", "Proof of Household Income" }));
        await bursaryApplicationRepository.AddAsync(
            new BursaryApplication(sipho.Id, "Funza Lushaka", 60000m, DateTime.UtcNow.AddMonths(1),
                new[] { "Certified ID Copy", "Academic Transcript" }));
    }

    // Week 6 Day 1: three accounts to demo with — one per role. The password
    // comes from user-secrets, never from source code.
    if (thandiwe is not null && !await dbContext.Users.AnyAsync())
    {
        var seedPassword = builder.Configuration["SeedUsers:Password"]
            ?? throw new InvalidOperationException(
                "SeedUsers:Password was not found. Run 'dotnet user-secrets set " +
                "SeedUsers:Password \"...\"' inside the API project.");
        var passwordHasher = services.GetRequiredService<IPasswordHasher<AppUser>>();
        var userRepository = services.GetRequiredService<IUserRepository>();

        var seedUsers = new[]
        {
            new AppUser("admin@matric-compass.test", Roles.Admin),
            new AppUser("counsellor@matric-compass.test", Roles.Counsellor),
            new AppUser("thandiwe@matric-compass.test", Roles.Learner, thandiwe.Id)
        };
        foreach (var user in seedUsers)
        {
            user.SetPasswordHash(passwordHasher.HashPassword(user, seedPassword));
            await userRepository.AddAsync(user);
        }
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
