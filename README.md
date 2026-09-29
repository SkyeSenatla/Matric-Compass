# Matric Compass

An API that helps South African matric (Grade 12) students track their subjects, tertiary applications, bursary applications, and aptitude test results — and get career recommendations based on those results.

Built incrementally as a teaching project: each week/day adds one deliberate concept on top of a working API, rather than shipping a finished app up front.

## Tech stack

- **.NET 10** / ASP.NET Core Web API (controllers, not minimal APIs — see `Program.cs` for why)
- **FluentValidation** for request-shape validation
- **Microsoft.AspNetCore.OpenApi** + **Scalar** for API documentation (`/scalar/v1`)
- **xUnit** + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) for tests
- In-memory storage only, for now — see [Current limitations](#current-limitations)

## Project structure

```
Domain/            Entities and domain exceptions — no framework dependencies
  Entities/         Student, TertiaryApplication, BursaryApplication, AptitudeTest
  Exceptions/       DomainException, NotFoundException, ConflictException, UnprocessableEntityException

API/                The web project
  Controllers/      One controller per entity — Students, TertiaryApplications, BursaryApplications, AptitudeTests
  Services/         Business rules that span more than one entity or need a repository read first
  Data/             IRepository<T> + an in-memory implementation per entity
  Validation/       FluentValidation validators for incoming request DTOs
  Models/           Request/response DTOs — entities never cross the HTTP boundary directly
  Common/           DomainExceptionHandler — the single catch site for every domain failure

API.Tests/          xUnit test project
  Services/          Unit tests — business rules tested against the service class directly, no HTTP
  ErrorShapeTests     Integration tests proving each failure mode returns the right ProblemDetails shape
  HappyPathTests      Integration tests proving success paths work end to end through the real pipeline
  IdempotencyTests     Integration tests for idempotency guarantees and validation boundary conditions
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

```bash
dotnet run --project API
```

- Swagger/OpenAPI JSON: `http://localhost:<port>/openapi/v1.json`
- Interactive docs (Scalar): `http://localhost:<port>/scalar/v1`

The app seeds two students (with subjects, a tertiary application, and a bursary application each) on startup — see the bottom of `API/Program.cs`.

## Testing

```bash
dotnet test
```

Tests are split by what they're proving, not just by folder:

- **Unit tests** (`API.Tests/Services/`) — a service class and a real in-memory repository, no HTTP, no DI container. Fast, and pinned to one business rule each.
- **Integration tests** (`ErrorShapeTests`, `HappyPathTests`, `IdempotencyTests`) — boot the real app via `WebApplicationFactory<Program>` and drive it over HTTP, proving the full pipeline (binding → validation → service → repository → serialization) for both failure and success paths, plus idempotency and validation-boundary edge cases.

A refactor that changes *how* something works (e.g. the in-memory repositories' internal storage) without changing *what it promises* should not require any test above to change. If it does, that test was coupled to an implementation detail, not a behavior.

## Current limitations

This is a teaching snapshot, not a finished product. Known gaps, left in deliberately as the next round of work:

- **Storage is in-memory only.** Every repository is a `Dictionary<Guid, T>` living in process memory (`API/Data/InMemoryRepository.cs`) — restarting the app loses all data. A real database (EF Core + PostgreSQL) is the planned replacement; the code is already split behind repository interfaces so that swap shouldn't touch controllers or services.
- **No authentication/authorization.** Every endpoint is open.
- **`TertiaryApplication` is read-only** — no create/update/delete endpoint yet, and no service layer for advancing an application through its status pipeline (`Researching` → ... → `Accepted`/`Rejected`).
- **Not every endpoint has a `[ProducesResponseType]`/XML summary yet** — every `GetAll` action (across all four controllers) is undocumented and untested by design, as a standing exercise in applying the same pattern already used everywhere else.
