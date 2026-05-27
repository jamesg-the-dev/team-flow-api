using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Api.Middleware;

/// <summary>
/// Translates domain/application exceptions into RFC 7807 ProblemDetails responses.
/// Result-pattern failures are surfaced by controllers via <see cref="ResultExtensions"/>.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(ctx, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext ctx, Exception ex)
    {
        var (status, title, code) = ex switch
        {
            DomainException de => (StatusCodes.Status409Conflict, de.Message, de.Code),
            ValidationException ve => (StatusCodes.Status400BadRequest, ve.Message, "validation"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "unexpected"),
        };

        if (status >= 500)
            _logger.LogError(ex, "Unhandled exception while processing request {Path}", ctx.Request.Path);
        else
            _logger.LogInformation(ex, "Request failed: {Code}", code);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://teamflow.app/problems/{code}",
            Instance = ctx.Request.Path,
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = ctx.TraceIdentifier;

        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(problem);
    }
}

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : Problem(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? (onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value))
            : Problem(result.Error);

    public static ActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : ProblemAction(result.Error);

    public static ActionResult<T> ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ProblemAction(result.Error);

    private static IResult Problem(Error error)
    {
        var status = StatusFromError(error);
        return Results.Problem(title: error.Message, statusCode: status,
            type: $"https://teamflow.app/problems/{error.Code}",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    private static ObjectResult ProblemAction(Error error)
    {
        var status = StatusFromError(error);
        var pd = new ProblemDetails
        {
            Status = status, Title = error.Message,
            Type = $"https://teamflow.app/problems/{error.Code}",
        };
        pd.Extensions["code"] = error.Code;
        return new ObjectResult(pd) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }

    private static int StatusFromError(Error error) => error.Type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest,
    };
}
