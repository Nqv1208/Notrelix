using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Application.Features.Identity.Security.DTOs;

/// <summary>
/// Read model of the signed-in user's own sign-in security facts for the account security page.
/// Contains no secret, hash, token or provider subject. Sessions and devices are listed by
/// <c>Identity.Sessions.List</c>; this reports only the counts that summarise them.
/// </summary>
/// <param name="ServerTime">Producer clock, so the client can render relative times without trusting its own.</param>
/// <param name="HasPasswordCredential">False for an account that only signs in through a linked provider.</param>
/// <param name="PasswordChangedAt">Null when the password has not been changed since registration (not tracked before then).</param>
/// <param name="Mfa">Same facts as <c>Identity.Mfa.Status</c>.</param>
/// <param name="ActiveSessionCount">Sessions that are active and not expired, the caller's own included.</param>
/// <param name="LinkedProviders">Names of linked sign-in providers (for example <c>Google</c>), never their subject ids.</param>
public sealed record AccountSecuritySummaryDto(
    DateTimeOffset ServerTime,
    bool HasPasswordCredential,
    DateTimeOffset? PasswordChangedAt,
    MfaStatusDto Mfa,
    int ActiveSessionCount,
    IReadOnlyList<string> LinkedProviders);
