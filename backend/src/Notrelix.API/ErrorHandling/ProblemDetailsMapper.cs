using AppForbiddenException = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppBusinessRuleException = Notrelix.Application.Common.Exceptions.BusinessRuleException;
using AppConflictException = Notrelix.Application.Common.Exceptions.ConflictException;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using DomainNotFoundException = Notrelix.Domain.Common.Exceptions.NotFoundException;
using DomainForbiddenException = Notrelix.Domain.Common.Exceptions.ForbiddenException;
using DomainBusinessRuleViolationException = Notrelix.Domain.Common.Exceptions.BusinessRuleViolationException;
using DomainConflictException = Notrelix.Domain.Common.Exceptions.ConflictException;
using DomainValidationException = Notrelix.Domain.Common.Exceptions.DomainValidationException;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Notrelix.API.ErrorHandling;

public static class ProblemDetailsMapper
{
    private static bool IsWorkspaceSlugUniqueViolation(Exception exception)
    {
        return exception is DbUpdateException { InnerException: PostgresException pg } &&
               pg.SqlState == PostgresErrorCodes.UniqueViolation &&
               pg.ConstraintName == "ux_workspaces_account_slug_active";
    }

    public static ProblemDetails Map(HttpContext context, Exception exception)
    {
        (int StatusCode, string ErrorCode, string Title, string Detail, IReadOnlyDictionary<string, string[]>? Errors) mapped = exception switch
        {
            _ when IsWorkspaceSlugUniqueViolation(exception) => (
                StatusCodes.Status409Conflict,
                ErrorCodes.Conflict,
                "Slug conflict",
                "A workspace with this slug already exists in your account.",
                null
            ),
            FluentValidation.ValidationException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Validation failed",
                "One or more validation errors occurred.",
                (IReadOnlyDictionary<string, string[]>)ex.Errors
            ),
            Notrelix.Application.Common.Exceptions.ValidationException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Validation failed",
                "One or more validation errors occurred.",
                (IReadOnlyDictionary<string, string[]>)ex.Errors
            ),
            DomainValidationException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Validation failed",
                "One or more domain validation errors occurred.",
                ex.Errors
            ),
            AppBusinessRuleException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.BusinessRuleViolation,
                "Business rule violation",
                ex.Message,
                null
            ),
            DomainBusinessRuleViolationException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.BusinessRuleViolation,
                "Business rule violation",
                ex.Message,
                null
            ),
            UnauthorizedException => (
                StatusCodes.Status401Unauthorized,
                ErrorCodes.Unauthorized,
                "Unauthorized",
                exception.Message,
                null
            ),
            AppForbiddenException => (
                StatusCodes.Status403Forbidden,
                ErrorCodes.Forbidden,
                "Forbidden",
                exception.Message,
                null
            ),
            DomainForbiddenException => (
                StatusCodes.Status403Forbidden,
                ErrorCodes.Forbidden,
                "Forbidden",
                exception.Message,
                null
            ),
            AppNotFoundException => (
                StatusCodes.Status404NotFound,
                ErrorCodes.ResourceNotFound,
                "Resource not found",
                exception.Message,
                null
            ),
            DomainNotFoundException => (
                StatusCodes.Status404NotFound,
                ErrorCodes.ResourceNotFound,
                "Resource not found",
                exception.Message,
                null
            ),
            AppConflictException => (
                StatusCodes.Status409Conflict,
                ErrorCodes.Conflict,
                "Conflict",
                exception.Message,
                null
            ),
            DomainConflictException => (
                StatusCodes.Status409Conflict,
                ErrorCodes.Conflict,
                "Conflict",
                exception.Message,
                null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorCodes.InternalServerError,
                "Internal server error",
                "An unexpected error occurred.",
                null
            )
        };

        var problemDetails = new ProblemDetails
        {
            Type = $"https://docs.notrelix.com/problems/{mapped.ErrorCode.Replace('.', '-')}",
            Title = mapped.Title,
            Status = mapped.StatusCode,
            Detail = mapped.Detail,
            Instance = context.Request.Path,
        };

        problemDetails.Extensions["errorCode"] = mapped.ErrorCode;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (mapped.Errors is { Count: > 0 })
        {
            problemDetails.Extensions["errors"] = mapped.Errors;
        }

        return problemDetails;
    }
}
