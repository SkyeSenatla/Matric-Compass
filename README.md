# Matric Compass

An API that helps South African matric (Grade 12) students track their subjects, tertiary applications, bursary applications, and aptitude test results — and get career recommendations based on those results.

Built incrementally as a teaching project: each week/day adds one deliberate concept on top of a working API, rather than shipping a finished app up front.

## Tech stack

- **.NET 10** / ASP.NET Core Web API (controllers, not minimal APIs — see `Program.cs` for why)
- **FluentValidation** for request-shape validation
- **EF Core** + **PostgreSQL** (Npgsql) for `Student` and `BursaryApplication` persistence; `TertiaryApplication` and `AptitudeTest` are still in-memory — see [Current limitations](#current-limitations)
- **Microsoft.AspNetCore.OpenApi** + **Scalar** for API documentation (`/scalar/v1`)
- **xUnit** + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) for tests, against a **Testcontainers**-provisioned PostgreSQL instance — no manually-started database required to run the suite

## Project structure

```
Domain/            Entities and domain exceptions — no framework dependencies
  Entities/         Student, TertiaryApplication, BursaryApplication, AptitudeTest
  Exceptions/       DomainException, NotFoundException, ConflictException, UnprocessableEntityException

API/                The web project
  Controllers/      One controller per entity — Students, TertiaryApplications, BursaryApplications, AptitudeTests
  Services/         Business rules that span more than one entity or need a repository read first
  Data/             IRepository<T>; an in-memory implementation per entity, plus MatricCompassDbContext
                    and EfStudentRepository (the one entity backed by a real database so far)
  Migrations/       EF Core code-first migrations (git-tracked; review before running database update)
  Validation/       FluentValidation validators for incoming request DTOs
  Models/           Request/response DTOs — entities never cross the HTTP boundary directly
  Common/           DomainExceptionHandler — the single catch site for every domain failure

API.Tests/          xUnit test project
  Services/          Unit tests — business rules tested against the service class directly, no HTTP
  ErrorShapeTests     Integration tests proving each failure mode returns the right ProblemDetails shape
  HappyPathTests      Integration tests proving success paths work end to end through the real pipeline
  IdempotencyTests     Integration tests for idempotency guarantees and validation boundary conditions
  TransactionTests     Proves an explicit EF Core transaction rollback leaves no trace
```

## Domain model

| Entity | Belongs to | Notes |
|---|---|---|
| `Student` | — | Full name, learner reference number (LRN), enrolled subjects (max 7, no duplicates) |
| `TertiaryApplication` | a Student | Institution + programme; read-only for now (no create/update/delete endpoint yet) |
| `BursaryApplication` | a Student | Funder, amount, deadline, required documents, status |
| `AptitudeTest` | a Student | Test type, date, score (0–100); generates `RecommendedCareers` from the score at creation time |

Every entity validates its own invariants in its constructor (e.g. a `BursaryApplication` can't have a negative amount). Rules that need to look at *other* records — no duplicate active bursary application with the same funder, no duplicate student LRN — live in the service layer instead, because a single entity can't see the rest of the dataset.

## API endpoints

| Method | Route | Notes |
|---|---|---|
| GET | `/api/students` | |
| GET | `/api/students/{id}` | 404 if not found |
| POST | `/api/students` | 409 on duplicate LRN |
| PUT | `/api/students/{id}` | Updates full name only |
| DELETE | `/api/students/{id}` | Idempotent in effect (204 then 404) |
| GET | `/api/tertiary-applications` | |
| GET | `/api/tertiary-applications/{id}` | 404 if not found |
| GET | `/api/bursary-applications` | |
| GET | `/api/bursary-applications/{id}` | 404 if not found |
| POST | `/api/bursary-applications` | 409 duplicate active funder, 422 deadline in the past |
| PUT | `/api/bursary-applications/{id}` | Updates amount and deadline only |
| DELETE | `/api/bursary-applications/{id}` | |
| GET | `/api/aptitude-tests` | |
| GET | `/api/aptitude-tests/{id}` | 404 if not found |
| POST | `/api/aptitude-tests` | Generates `RecommendedCareers` from the score |

`Students` and `TertiaryApplications` currently have no matching create/update/delete pair for tertiary applications — that's a known gap, not an oversight (see below).

Full request/response examples: [API/API.http](API/API.http).

## Error handling

Every domain failure is thrown as an exception and caught in exactly one place — `DomainExceptionHandler` (`API/Common/DomainExceptionHandler.cs`). No controller action contains a `try`/`catch`. The mapping:

| Exception | Status | When |
|---|---|---|
| `FluentValidation.ValidationException` | 400 | Request body fails shape validation |
| `ArgumentException` | 400 | An entity's own constructor/method guard rejects the input |
| `NotFoundException` | 404 | Requested id doesn't exist |
| `ConflictException` | 409 | A business rule about duplicates is violated |
| `UnprocessableEntityException` | 422 | The request is well-formed but semantically invalid (e.g. a deadline in the past) |
| anything else | 500 | Unexpected — logged with a correlation id, returned as `application/problem+json` |

Every documented status code above is also declared on its controller action via `[ProducesResponseType]`, and most actions carry XML doc comments that Scalar reads directly — so `/scalar/v1` reflects what the API actually does, not just its happy path.

## Running the API

`Student` is backed by a real PostgreSQL database now, so there's a one-time setup step before `dotnet run` works:

```bash
# 1. Start a local Postgres instance (Docker)
docker run --name matric-compass-postgres -e POSTGRES_PASSWORD=devpassword -p 5432:5432 -d postgres:17

# 2. Point the app at it via user secrets (never appsettings.json — see below)
cd API
dotnet user-secrets set "ConnectionStrings:MatricCompass" "Host=localhost;Port=5432;Database=matric_compass;Username=postgres;Password=devpassword"

# 3. Run — migrations apply automatically on startup (Database.MigrateAsync())
dotnet run
```

- Swagger/OpenAPI JSON: `http://localhost:<port>/openapi/v1.json`
- Interactive docs (Scalar): `http://localhost:<port>/scalar/v1`

The app seeds two students (with subjects, a tertiary application, and a bursary application each) on startup, idempotently — see the bottom of `API/Program.cs`. `TertiaryApplication` and `BursaryApplication` reset every restart (still in-memory); `Student` rows persist in Postgres across restarts.

To generate a new migration after changing an entity or `MatricCompassDbContext`:

```bash
dotnet ef migrations add <Name> --project API --startup-project API
dotnet ef database update --project API --startup-project API
```

Always read a generated migration before running it against a shared database — it's the one reviewable artifact that catches a silent drop-and-recreate hiding behind what looks like a rename.

## Testing

```bash
dotnet test
```

That's the whole prerequisite — no manual `docker run` for Postgres, no connection string to set. `API.Tests/TestSupport/PostgresApiFactory.cs` starts a disposable PostgreSQL container (via Testcontainers) once per test run, points the app at it, and tears it down when the run finishes. The only requirement is a running Docker daemon.

Tests are split by what they're proving, not just by folder:

- **Unit tests** (`API.Tests/Services/`) — a service class and a real in-memory repository, no HTTP, no DI container, no database at all. Fast, and pinned to one business rule each.
- **Integration tests** (`ErrorShapeTests`, `HappyPathTests`, `IdempotencyTests`, `PaginationTests`, `QueryBehaviorTests`, `ConstraintTests`, `ConcurrencyTests`, `TransactionTests`) — all share one `[Collection("Postgres collection")]` fixture, booting the real app once via `PostgresApiFactory` and driving it over HTTP, proving the full pipeline (binding → validation → service → repository → serialization) for failure paths, success paths, pagination, database constraints, optimistic concurrency, and idempotency.

A refactor that changes *how* something works (e.g. the in-memory repositories' internal storage, or swapping the dev Postgres container for a disposable one) without changing *what it promises* should not require any test above to change. If it does, that test was coupled to an implementation detail, not a behavior.

## Current limitations

This is a teaching snapshot, not a finished product. Known gaps, left in deliberately as the next round of work:

- **Only `Student` and `BursaryApplication` are backed by a real database.** `TertiaryApplication` and `AptitudeTest` are still `Dictionary<Guid, T>` in-memory repositories (`Infrastructure/Data/InMemoryRepository.cs`) — restarting the app resets them. `EfStudentRepository`/`EfBursaryApplicationRepository` are the template the same swap gets applied to for the other two.
- **`Student.SubjectCodes`, `BursaryApplication.RequiredDocuments`, and `AptitudeTest.RecommendedCareers` are not persisted.** They're computed, read-only wrapper properties over a private field, which EF Core can't map — `MatricCompassDbContext.OnModelCreating` explicitly `Ignore()`s all three for now. Concretely: seeded subjects currently do **not** survive a read back from Postgres (`GET /api/students` shows `subjectCodes: []`). The real fix is replacing `SubjectCodes` with a proper `Subject` entity and a one-to-many relationship, not a workaround to make the existing shape persist.
- **No authentication/authorization.** Every endpoint is open.
- **`TertiaryApplication` is read-only** — no create/update/delete endpoint yet, and no service layer for advancing an application through its status pipeline (`Researching` → ... → `Accepted`/`Rejected`).
- **Not every endpoint has a `[ProducesResponseType]`/XML summary yet** — every `GetAll` action (across all four controllers) is undocumented and untested by design, as a standing exercise in applying the same pattern already used everywhere else.
- **`orderBy=amount` has no supporting index** on `BursaryApplication` paging — it's on the sort allow-list, so at large volume it sorts the student's whole history instead of walking an index.
- **Page tokens are opaque by convention, not signed.** A client can decode one and build its own; the contract forbids it, but nothing enforces that.
- **`PUT /api/students/{id}` is still last-writer-wins.** Only `BursaryApplication` has an optimistic-concurrency (`xmin`) token so far.
