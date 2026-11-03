using Microsoft.AspNetCore.Mvc;

namespace PenangGoApi.Services;

/// <summary>
/// Uniform ProblemDetails responses for validation failures. The "message" extension
/// keeps clients that read a simple message string working.
/// </summary>
public static class ApiResponses
{
    public static IResult Problem(string message, int statusCode = StatusCodes.Status400BadRequest, string? field = null, IDictionary<string, object?>? extensions = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status409Conflict ? "Conflict" : "Invalid request",
            Detail = message
        };
        problem.Extensions["message"] = message;
        if (field != null) problem.Extensions["field"] = field;
        if (extensions != null)
        {
            foreach (var kv in extensions)
                problem.Extensions[kv.Key] = kv.Value;
        }
        return Results.Problem(problem);
    }
}