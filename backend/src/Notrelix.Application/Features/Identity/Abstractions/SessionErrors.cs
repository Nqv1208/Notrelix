using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Identity.Abstractions;

/// <summary>Errors every handler that acts for the signed-in person answers the same way.</summary>
public static class SessionErrors
{
    /// <summary>
    /// A valid session whose person no longer exists ends like any other dead session (401), so the
    /// client signs out instead of showing a "not found" for the person's own data.
    /// </summary>
    public static ApplicationError SessionUserMissing { get; } = new(
        "identity.auth.session-invalid",
        "Session is invalid or expired",
        ApplicationErrorType.Authentication);
}
