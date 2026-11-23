using CodingJamAPI.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Middleware;

/// <summary>Maps domain exceptions to RFC 7807 problem responses (errors keyed by camelCase field).</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; nothing to send.
        }
        catch (ValidationFailedException ex)
        {
            var problem = new ValidationProblemDetails(ex.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = ex.Message
            };
            await Write(context, problem);
        }
        catch (UnauthorizedException ex)
        {
            await Write(context, new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Unauthorized", Detail = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            await Write(context, new ProblemDetails { Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Detail = ex.Message });
        }
        catch (NotFoundException ex)
        {
            await Write(context, new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not found", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await Write(context, new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Server error", Detail = "Something went wrong." });
        }
    }

    private static Task Write(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }
}
