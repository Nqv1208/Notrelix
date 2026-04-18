using System.Text.Json;
using Notrelix.Application.Common.Exceptions;

namespace Notrelix.API.Middleware;

// Middleware xử lý exception toàn cục
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                new ErrorResponse
                {
                    Type = "ValidationError",
                    Message = "Validation failed",
                    Errors = validationEx.Errors
                }
            ),
            ArgumentException argumentEx => (
                StatusCodes.Status400BadRequest,
                new ErrorResponse
                {
                    Type = "ValidationError",
                    Message = argumentEx.Message,
                    Errors = new Dictionary<string, string[]>
                    {
                        ["request"] = new[] { argumentEx.Message }
                    }
                }
            ),
            UnauthorizedException => (
                StatusCodes.Status401Unauthorized,
                new ErrorResponse
                {
                    Type = "Unauthorized",
                    Message = exception.Message
                }
            ),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                new ErrorResponse
                {
                    Type = "NotFound",
                    Message = exception.Message
                }
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ErrorResponse
                {
                    Type = "InternalServerError",
                    Message = "An unexpected error occurred"
                }
            )
        };

        // Log error
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public class ErrorResponse
{
    public required string Type { get; init; }
    public required string Message { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
}

// Extension để đăng ký middleware
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
