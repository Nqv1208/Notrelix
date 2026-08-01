namespace Notrelix.Application.Common.Models;

public enum ApplicationErrorType
{
    Validation,
    NotFound,
    Conflict,
    PreconditionFailed,
    BusinessRule
}

public sealed record ApplicationError(
    string Code,
    string Message,
    ApplicationErrorType Type,
    string? Target = null);
