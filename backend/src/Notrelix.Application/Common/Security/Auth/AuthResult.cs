namespace Notrelix.Application.Common.Security.Auth;

public record AuthResult
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public UserDto? User { get; init; }
    public string WorkspaceProvisioning { get; init; } = "pending";

    /// <summary>
    /// True when the caller authenticated with valid credentials but the
    /// account requires an MFA challenge before a session can be issued.
    /// AccessToken/RefreshToken are null in this state.
    /// </summary>
    public bool MfaRequired { get; init; }

    /// <summary>Single-use challenge token to submit to the MFA verify endpoint.</summary>
    public string? MfaChallengeToken { get; init; }

    /// <summary>MFA factor to satisfy, e.g. "AuthenticatorApp".</summary>
    public string? MfaMethod { get; init; }

    /// <summary>UTC expiry of the challenge token.</summary>
    public DateTime? MfaExpiresAt { get; init; }
}

public record UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public string? AvatarUrl { get; init; }
    public bool EmailConfirmed { get; init; }
}
