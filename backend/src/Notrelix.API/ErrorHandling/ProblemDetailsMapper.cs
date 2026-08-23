using AppForbiddenException = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppBusinessRuleException = Notrelix.Application.Common.Exceptions.BusinessRuleException;
using AppConflictException = Notrelix.Application.Common.Exceptions.ConflictException;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppValidationException = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.API.ErrorHandling;

public static class ProblemDetailsMapper
{
    public static ProblemDetails Map(HttpContext context, Exception exception)
    {
        (int StatusCode, string ErrorCode, string Title, string Detail, IReadOnlyDictionary<string, string[]>? Errors) mapped = exception switch
        {
            FluentValidation.ValidationException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Validation failed",
                "One or more validation errors occurred.",
                (IReadOnlyDictionary<string, string[]>)ex.Errors
            ),
            AppValidationException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Validation failed",
                "One or more validation errors occurred.",
                (IReadOnlyDictionary<string, string[]>)ex.Errors
            ),
            AppBusinessRuleException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.BusinessRuleViolation,
                "Business rule violation",
                ex.Message,
                null
            ),
            Notrelix.Domain.Common.Exceptions.BusinessRuleException ex => (
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
            // Request-contract security violations (scoped request without an
            // authorization declaration, tenant context gaps) get their own
            // diagnosable category instead of a generic 500.
            SecurityMisconfigurationException => (
                StatusCodes.Status500InternalServerError,
                ErrorCodes.AuthorizationMisconfiguration,
                "Authorization misconfiguration",
                "The request could not be authorized due to a server-side contract violation.",
                null
            ),
            AppNotFoundException => (
                StatusCodes.Status404NotFound,
                ErrorCodes.ResourceNotFound,
                "Resource not found",
                exception.Message,
                null
            ),
            Notrelix.Application.Common.Idempotency.IdempotencyPayloadMismatchException => (
                StatusCodes.Status409Conflict,
                ErrorCodes.IdempotencyPayloadMismatch,
                "Idempotency conflict",
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
            Notrelix.Application.Common.Exceptions.PreconditionFailedException precondition => (
                StatusCodes.Status412PreconditionFailed,
                precondition.ErrorCode,
                "Precondition failed",
                precondition.Message,
                null
            ),
            Notrelix.Application.Common.Exceptions.AccountSelectionRequiredException => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.AccountSelectionRequired,
                "Account selection required",
                exception.Message,
                null
            ),
            Notrelix.Application.Common.Idempotency.IdempotencyIncompleteStateException => (
                StatusCodes.Status503ServiceUnavailable,
                ErrorCodes.IdempotencyStateIncomplete,
                "Service unavailable",
                "The operation is being processed. Retry shortly with the same Idempotency-Key.",
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
