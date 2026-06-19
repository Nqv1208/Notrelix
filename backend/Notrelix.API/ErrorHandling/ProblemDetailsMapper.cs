using Microsoft.AspNetCore.Mvc;
using Notrelix.Application.Common.Exceptions;
using AppForbiddenException = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppBusinessRuleException = Notrelix.Application.Common.Exceptions.BusinessRuleException;
using AppConflictException = Notrelix.Application.Common.Exceptions.ConflictException;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using DomainNotFoundException = Notrelix.Domain.Common.Exceptions.NotFoundException;
using DomainForbiddenException = Notrelix.Domain.Common.Exceptions.ForbiddenException;
using DomainBusinessRuleViolationException = Notrelix.Domain.Common.Exceptions.BusinessRuleViolationException;
using DomainConflictException = Notrelix.Domain.Common.Exceptions.ConflictException;
using DomainValidationException = Notrelix.Domain.Common.Exceptions.DomainValidationException;

namespace Notrelix.API.ErrorHandling;

public static class ProblemDetailsMapper
{
    public static ProblemDetails Map(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, title, detail, errors) = exception switch
        {
            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Validation failed",
                "One or more validation errors occurred.",
                ex.Errors
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
            Type = $"https://docs.notrelix.com/problems/{errorCode.Replace('.', '-')}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path,
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (errors is { Count: > 0 })
        {
            problemDetails.Extensions["errors"] = errors;
        }

        return problemDetails;
    }
}
