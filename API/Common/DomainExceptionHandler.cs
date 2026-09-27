namespace API.Common;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Domain.Exceptions;
using FluentValidation;

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
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        // httpContext.TraceIdentifier is a per-request id ASP.NET Core already
        // generates — putting it in both the log line and the response body
        // is what lets support match a parent's screen to an exact log line,
        // instead of guessing from a timestamp.
        _logger.LogError(exception,
            "Request {CorrelationId} failed with {Status}: {Message}",
            httpContext.TraceIdentifier, status, exception.Message);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = $"https://api.matric-compass.co.za/errors/{exception.GetType().Name}",
            Title = title,
            Status = status,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions = { ["correlationId"] = httpContext.TraceIdentifier }
        }, options: null, contentType: "application/problem+json", cancellationToken);

        return true; // "yes, I handled this — stop looking for another handler"
    }
}
