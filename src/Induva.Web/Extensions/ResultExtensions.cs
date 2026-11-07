using Induva.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Induva.Web.Extensions;

public static class ResultExtensions
{
    public static int ToStatusCode(this ErrorKind k) => k switch
    {
        ErrorKind.Validation => 400,
        ErrorKind.NotFound => 404,
        ErrorKind.Forbidden => 403,
        ErrorKind.RateLimited => 429,
        ErrorKind.AiUnavailable => 503,
        _ => 500
    };

    /// <summary>
    /// For AJAX endpoints: JSON { error } with the right status code.
    /// </summary>
    public static IActionResult ToJsonError(this Result r) =>
        new ObjectResult(new { error = r.Error }) { StatusCode = r.Kind.ToStatusCode() };
}

