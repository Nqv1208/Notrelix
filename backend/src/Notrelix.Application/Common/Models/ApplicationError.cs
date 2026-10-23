namespace Notrelix.Application.Common.Models;

public enum ApplicationErrorType
{
    Validation,
    Authentication,
    NotFound,
    Conflict,
    PreconditionFailed,
    BusinessRule,
    TooManyRequests
}

public sealed record ApplicationError(
    string Code,
    string Message,
    ApplicationErrorType Type,
    string? Target = null,
    TimeSpan? RetryAfter = null);
