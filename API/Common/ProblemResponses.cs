namespace API.Common;

using Microsoft.AspNetCore.Mvc;

// One error shape (RFC 9457) for the whole API. Every action calls this
// by hand, on purpose — same kind of deliberate repetition as the
// try/catch in CreateStudentAsync. Day 3 replaces the repetition with one
// piece of middleware that calls this instead of every action doing it
// individually. Today is only about agreeing on the shape.
public static class ProblemResponses
{
    public static ObjectResult Conflict(string detail, string instance) => Problem(
        type: "https://api.matric-compass.co.za/errors/conflict",
        title: "Conflict",
        status: StatusCodes.Status409Conflict,
        detail: detail,
        instance: instance);

    public static ObjectResult UnprocessableEntity(string detail, string instance) => Problem(
        type: "https://api.matric-compass.co.za/errors/unprocessable-entity",
        title: "Unprocessable entity",
        status: StatusCodes.Status422UnprocessableEntity,
        detail: detail,
        instance: instance);

    private static ObjectResult Problem(
        string type, string title, int status, string detail, string instance) =>
        new(new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Instance = instance
        })
        {
            StatusCode = status
        };
}
