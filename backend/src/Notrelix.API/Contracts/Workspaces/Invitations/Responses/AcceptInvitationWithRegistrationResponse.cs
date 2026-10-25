namespace Notrelix.API.Contracts.Workspaces.Invitations.Responses;

/// <summary>
/// Result of accepting an invitation without an account. The session of the new identity travels only
/// as httpOnly cookies (D10); <see cref="User"/> and <see cref="ExpiresAt"/> describe it. Both are null
/// on a repeat press, which answers the same success without signing anyone in.
/// </summary>
public sealed record AcceptInvitationWithRegistrationResponse
{
    public required string WorkspaceSlug { get; init; }
    public required Guid WorkspaceId { get; init; }

    /// <summary>Kind of the one Board or Page a resource invitation opened; null for a plain Workspace invitation.</summary>
    public string? ResourceKind { get; init; }
    public Guid? ResourceId { get; init; }

    /// <summary>True when the invitation had already been accepted.</summary>
    public required bool AlreadyAccepted { get; init; }

    public DateTime? ExpiresAt { get; init; }
    public UserDto? User { get; init; }
}
