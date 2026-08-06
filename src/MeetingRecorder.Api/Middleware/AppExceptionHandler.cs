using MeetingRecorder.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRecorder.Api.Middleware;

public class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, exception.Message),
            ValidationFailedException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            logger.LogError(exception, "Unhandled exception processing {Path}", httpContext.Request.Path);
            return false;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title
        }, ct);

        return true;
    }
}
