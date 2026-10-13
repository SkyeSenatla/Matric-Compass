namespace API.Common;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;
using API.Auth;
using API.Observability;

// The one catch site for every domain failure in the app. Day 2's
// ProblemResponses required every action to call it by hand, action by
// action; TryHandleAsync below is called automatically for any exception
// that escapes the request pipeline, so a new failure mode needs a new
// case in the switch here, never a new try/catch in a controller.
public class DomainExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger;

    public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Bad Request"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            UnprocessableEntityException => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
            // Week 6 Day 1: "we don't know who you are" — bad login, or a
            // refresh token that's missing, unknown, expired or already used.
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            // Week 5 Day 3: optimistic concurrency — the row changed since the
            // client read it. MUST come before the DbUpdateException case
            // below: DbUpdateConcurrencyException IS a DbUpdateException, and
            // a switch takes the first pattern that matches.
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict"),
            // Week 5 Day 3: a database constraint caught what the C# check
            // missed (usually two requests racing past it). 23505 is
            // PostgreSQL's SQLSTATE for unique_violation — it's the same
            // conflict ConflictException describes, so it gets the same 409.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
                => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        // Week 6 Day 2: the W3C trace id, not ASP.NET Core's local request id.
        // The same value is on every log line and every span of this request,
        // so a learner reading it off an error screen leads straight to the
        // trace in the dashboard.
        var correlationId = Correlation.GetId(httpContext);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            // A real fault in OUR code: log everything, stack trace included.
            _logger.LogError(exception,
                "Request {CorrelationId} failed with {Status}", correlationId, status);
        }
        else
        {
            // A 4xx is the client's mistake, not a fault — Information, not
            // Error, so real errors aren't buried. And no exception.Message:
            // 4xx messages often echo what the client sent ("A student with
            // LRN LRN-2026-00114 already exists"), and that's a learner's
            // personal information. The exception TYPE says what went wrong.
            _logger.LogInformation(
                "Request {CorrelationId} rejected with {Status} ({ExceptionType})",
                correlationId, status, exception.GetType().Name);
        }

        // Week 6 Day 3: a validation failure gets the SAME shape as the
        // framework's own model-binding 400 — an "errors" map from field name
        // to messages — so a form can put each message under its own field.
        // Names are camelCased to match the JSON the client actually sent.
        var problem = exception is ValidationException validationException
            ? new ValidationProblemDetails(validationException.Errors
                .GroupBy(error => JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()))
            : new ProblemDetails();

        problem.Type = $"https://api.matric-compass.co.za/errors/{exception.GetType().Name}";
        problem.Title = title;
        problem.Status = status;
        // A 500's message comes from deep inside our code or a library — a SQL
        // error, a file path, sometimes data. The client gets the correlation
        // id instead; the details stay in our logs. A validation failure's
        // messages are in "errors", so its detail is one plain sentence.
        problem.Detail = status >= StatusCodes.Status500InternalServerError
            ? "Something went wrong on our side. Quote the correlationId if you report this."
            : exception is ValidationException
                ? "One or more fields are invalid."
                : exception.Message;
        problem.Instance = httpContext.Request.Path;
        problem.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = status;
        // problem.GetType(), not ProblemDetails: serialize what the object
        // really is, or the "errors" property is silently left out.
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null,
            contentType: "application/problem+json", cancellationToken);

        return true; // "yes, I handled this — stop looking for another handler"
    }
}
