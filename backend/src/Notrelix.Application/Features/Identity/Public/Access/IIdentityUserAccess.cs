namespace Notrelix.Application.Features.Identity.Public.Access;

/// <summary>How a person's access ends at the Identity level.</summary>
public enum UserAccessEnd
{
    Deactivated,
    Suspended
}

/// <summary>
/// Producer-owned public surface for ending a person's access everywhere. Who may disable whom (an account
/// administrator, SCIM deprovisioning) is an Accounts decision: the caller decides, then calls this in the
/// same transaction, under a scope that may write the person's rows.
/// </summary>
public interface IIdentityUserAccess
{
    /// <summary>
    /// Moves the person to the status, then revokes every session (so no refresh succeeds), every API token in
    /// every workspace, and every access token and realtime connection already issued (a revoked-before
    /// watermark). Disabling a person who is already disabled changes nothing but still finishes any
    /// revocation left over, so a retry is safe. Returns false when no such person exists.
    /// </summary>
    Task<bool> EndAccessAsync(
        Guid userId,
        Guid endedBy,
        UserAccessEnd kind,
        string? reason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes every second factor and recovery code of the person, ends every session (so a remembered device
    /// cannot refresh), cuts off issued access tokens, records one activity line and mails the person. The
    /// caller (the workspace Owner path) has already decided who may do this and spent the step-up proof.
    /// Returns false when no such person exists.
    /// </summary>
    Task<bool> ResetMfaAsync(Guid userId, Guid resetBy, CancellationToken cancellationToken);
}
