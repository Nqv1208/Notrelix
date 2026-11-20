namespace Notrelix.API.Contracts.Identity;

/// <summary>
/// Browser-facing result of a credential step (register, login, MFA completion,
/// refresh). Session tokens are delivered only as httpOnly cookies and are
/// deliberately not part of this body (D10): a script can never read them.
/// When <see cref="MfaRequired"/> is true no session exists yet and the client
/// must submit <see cref="MfaChallengeToken"/> with the code to the MFA verify
/// operation.
/// </summary>
public sealed record AuthSessionResponse
{
    /// <summary>UTC expiry of the access token carried by the cookie. Null while an MFA challenge is pending.</summary>
    public DateTime? ExpiresAt { get; init; }

    public UserDto? User { get; init; }

    public string WorkspaceProvisioning { get; init; } = "pending";

    public bool MfaRequired { get; init; }

    /// <summary>Single-use challenge token for the MFA verify operation.</summary>
    public string? MfaChallengeToken { get; init; }

    /// <summary>Factor to satisfy, for example "AuthenticatorApp".</summary>
    public string? MfaMethod { get; init; }

    public DateTime? MfaExpiresAt { get; init; }

    /// <summary>
    /// True when the person's Account requires MFA and they have no factor: no session exists yet and the
    /// client must run the required-enrollment operations with <see cref="MfaEnrollmentToken"/>.
    /// </summary>
    public bool MfaEnrollmentRequired { get; init; }

    /// <summary>Single-use token accepted only by the required-enrollment operations.</summary>
    public string? MfaEnrollmentToken { get; init; }

    /// <summary>One-time recovery codes, present only on the answer that finished required enrollment.</summary>
    public IReadOnlyList<string>? RecoveryCodes { get; init; }

    public static AuthSessionResponse From(AuthResult result) => new()
    {
        ExpiresAt = result.ExpiresAt,
        User = result.User,
        WorkspaceProvisioning = result.WorkspaceProvisioning,
        MfaRequired = result.MfaRequired,
        MfaChallengeToken = result.MfaChallengeToken,
        MfaMethod = result.MfaMethod,
        MfaExpiresAt = result.MfaExpiresAt,
        MfaEnrollmentRequired = result.MfaEnrollmentRequired,
        MfaEnrollmentToken = result.MfaEnrollmentToken,
        RecoveryCodes = result.RecoveryCodes
    };
}
