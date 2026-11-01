namespace Notrelix.Application.Features.Governance.Abstractions;

/// <summary>
/// Reads the explicit access entries of one resource for the access summary:
/// its active User/Team resource permissions and the active Workspace members
/// each explicit source reaches (Workspace Owners, board members, User grants,
/// members of active granted Teams, and the direct User/Team entries of the
/// resource's Space), and whether a read permission rule applies.
/// Executed on the request's data session, so tenant RLS applies.
/// </summary>
public interface IResourceAccessEntriesReader
{
    Task<ResourceAccessEntries> ReadAsync(ResourceAccessEntriesRequest request, CancellationToken cancellationToken);
}

/// <param name="SpaceId">The Space whose direct entries reach the resource, or null when none does.</param>
/// <param name="MemberUserIds">The resource's members, as published by the owning context.</param>
/// <param name="ReadAction">The single-read action whose permission rules are checked (e.g. <c>ViewBoard</c>).</param>
public sealed record ResourceAccessEntriesRequest(
    Guid AccountId,
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    Guid? SpaceId,
    IReadOnlyCollection<Guid> MemberUserIds,
    string ReadAction);

/// <param name="SpaceName">Name of <see cref="ResourceAccessEntriesRequest.SpaceId"/> when it exists.</param>
/// <param name="HasReadRules">
/// An active permission rule for the read action applies to the resource
/// (Workspace scope or matching the resource), for any subject.
/// </param>
public sealed record ResourceAccessEntries(
    IReadOnlyList<ResourceGrantEntry> Grants,
    IReadOnlyList<AccessReachEntry> Reach,
    string? SpaceName,
    bool HasReadRules = false,
    IReadOnlyList<SpaceRoleEntry>? SpaceRoles = null);

/// <summary>The highest role of any direct or Team entry of the resource's Space that reaches a person.</summary>
/// <param name="RoleRank">Viewer=1, Member=2, Admin=3.</param>
public sealed record SpaceRoleEntry(Guid UserId, int RoleRank);

/// <param name="SubjectType"><c>User</c> or <c>Team</c> (only active Teams are returned).</param>
/// <param name="Rank">Viewer=1, Commenter=2, Editor=3, Manager=4, Owner=5, None=0.</param>
/// <param name="PermissionId">The resource permission row, for a write by id.</param>
/// <param name="GrantedAt">When the entry was created.</param>
/// <param name="TeamPeople">Active members of a Team subject; null for a User.</param>
/// <param name="PermissionVersion">The row's version, which a change of its level must name.</param>
public sealed record ResourceGrantEntry(
    string SubjectType,
    Guid SubjectId,
    int Rank,
    Guid? GrantedBy,
    string? TeamName,
    Guid? PermissionId = null,
    DateTimeOffset? GrantedAt = null,
    int? TeamPeople = null,
    long? PermissionVersion = null);

/// <summary>One active Workspace member reached by one explicit source.</summary>
/// <param name="Source">One of <see cref="AccessReachSources"/>.</param>
/// <param name="TeamId">The granted Team for <see cref="AccessReachSources.Team"/>; otherwise null.</param>
public sealed record AccessReachEntry(string Source, Guid? TeamId, Guid UserId, bool IsGuest);

public static class AccessReachSources
{
    /// <summary>An active Workspace Owner (reaches every resource, GOV-025 step 2).</summary>
    public const string Owner = "owner";
    public const string Member = "member";
    public const string User = "user";
    public const string Team = "team";
    public const string Space = "space";
}
