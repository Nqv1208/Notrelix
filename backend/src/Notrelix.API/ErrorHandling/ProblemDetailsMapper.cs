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
            Notrelix.Application.Common.Exceptions.PayloadTooLargeException => (
                StatusCodes.Status413PayloadTooLarge,
                ErrorCodes.AttachmentPayloadTooLarge,
                "Payload too large",
                exception.Message,
                null
            ),
            Microsoft.AspNetCore.Http.BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } => (
                StatusCodes.Status413PayloadTooLarge,
                ErrorCodes.AttachmentPayloadTooLarge,
                "Payload too large",
                "The request body exceeds the maximum allowed size.",
                null
            ),
            Microsoft.AspNetCore.Http.BadHttpRequestException or System.IO.InvalidDataException => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Validation failed",
                "The request body is malformed or was interrupted.",
                null
            ),
            Notrelix.Application.Common.Exceptions.UnsupportedMediaTypeException => (
                StatusCodes.Status415UnsupportedMediaType,
                ErrorCodes.AttachmentUnsupportedMediaType,
                "Unsupported media type",
                exception.Message,
                null
            ),
            Notrelix.Domain.Common.Exceptions.BusinessRuleException { RuleCode: Notrelix.Domain.Collaboration.CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded } ex => (
                StatusCodes.Status409Conflict,
                ErrorCodes.AttachmentLimitExceeded,
                "Attachment limit reached",
                ex.Message,
                null
            ),
            Notrelix.Domain.Common.Exceptions.BusinessRuleException { RuleCode: Notrelix.Domain.Collaboration.CollaborationRuleCodes.Collaboration_Attachment_FileSizeExceeded } ex => (
                StatusCodes.Status413PayloadTooLarge,
                ErrorCodes.AttachmentPayloadTooLarge,
                "Payload too large",
                ex.Message,
                null
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
            AppConflictException conflict => (
                StatusCodes.Status409Conflict,
                conflict.ErrorCode ?? ErrorCodes.Conflict,
                "Conflict",
                exception.Message,
                null
            ),
            Notrelix.Application.Features.Billing.Public.Capacity.CapacityOperationConflictException => (
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
