namespace Notrelix.Application.Features.Identity.Verification.Abstractions;

public sealed record EmailVerificationTokenIssue(
    Guid VerificationTokenId,
    Guid UserId,
    string Email,
    string ProtectedToken,
    int HashVersion,
    DateTimeOffset ExpiresAt);

public interface IEmailVerificationTokenIssuer
{
    Task<EmailVerificationTokenIssue> IssueAsync(
        User user,
        Guid actorUserId,
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken);

    /// <summary>Issues the link for the address a sign-in email change waits on and mails it to that address.</summary>
    Task<EmailVerificationTokenIssue> IssueForPendingEmailAsync(
        User user,
        string pendingNormalizedEmail,
        Guid actorUserId,
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken);
}
