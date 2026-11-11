using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Channels.Services;

/// <summary>
/// Who may see and change a channel. The pipeline admits the caller to the Workspace and checks the action; the
/// rules that depend on the channel's own membership are decided here, so every handler reads the same answer.
/// A channel the caller may not see is the same NotFound as a missing one, and Owner and Admin do not see a
/// private channel by role (CHT-004).
/// </summary>
public static class ChannelAccess
{
    public const string GuestRole = "Guest";

    /// <summary>The caller's standing in the Workspace. The pipeline already admitted them, so a missing fact is a missing channel.</summary>
    public static async Task<WorkspaceMemberFact> CallerAsync(
        IWorkspaceMemberFacts members, Guid workspaceId, Guid userId, CancellationToken ct) =>
        await members.FindAsync(workspaceId, userId, ct)
        ?? throw new NotFoundException(nameof(Space), workspaceId);

    /// <summary>A live channel of the Workspace with its memberships loaded, or the same NotFound as a missing one.</summary>
    public static async Task<Space> LoadLiveAsync(IChatDbContext db, Guid workspaceId, Guid channelId, CancellationToken ct)
    {
        var space = await db.ChatSpaces.FirstOrDefaultAsync(
            s => s.Id == channelId && s.WorkspaceId == workspaceId && s.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Space), channelId);

        // Loading the rows through the DbSet lets EF fix up the aggregate's membership collection.
        await db.ChatMemberships.Where(m => m.SpaceId == channelId).ToListAsync(ct);
        return space;
    }

    /// <summary>
    /// A live channel of the Workspace (not a direct or group conversation): the channel-only commands load through here, so a
    /// conversation is the same NotFound as a missing channel. Messages, pins and reactions load through <see cref="LoadLiveAsync"/>,
    /// which serves every kind of space.
    /// </summary>
    public static async Task<Space> LoadChannelAsync(IChatDbContext db, Guid workspaceId, Guid channelId, CancellationToken ct)
    {
        var space = await LoadLiveAsync(db, workspaceId, channelId, ct);
        if (space.IsConversation)
            throw new NotFoundException(nameof(Space), channelId);
        return space;
    }

    /// <summary>
    /// A private channel and a conversation are seen by their members only. A public channel is seen by every non-guest member; a
    /// Guest sees a public channel only when they were added to it.
    /// </summary>
    public static void RequireVisible(Space space, WorkspaceMemberFact caller, Guid userId)
    {
        var mustBeMember = !space.IsPublic || string.Equals(caller.Role, GuestRole, StringComparison.Ordinal);
        if (mustBeMember && !space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), space.Id);
    }

    /// <summary>Renaming, archiving, moving and removing others belong to a channel manager, a Workspace Owner or Admin.</summary>
    public static void RequireManager(Space space, WorkspaceMemberFact caller, Guid userId)
    {
        if (space.IsActiveManager(userId)) return;
        if (caller.Role is "Owner" or "Admin") return;

        throw new ForbiddenException("Only a channel manager, a Workspace Owner or Admin can change this channel.");
    }

    /// <summary>Channel names are unique per Workspace among active (not archived, not deleted) channels, ignoring case.</summary>
    public static async Task RequireUniqueNameAsync(IChatDbContext db, Guid workspaceId, string name, Guid? exceptChannelId, CancellationToken ct)
    {
        var key = name.Trim().ToLowerInvariant();
        var clash = await db.ChatSpaces.AnyAsync(s =>
            s.WorkspaceId == workspaceId
            && s.DeletedAt == null
            && s.ArchivedAt == null
            && (s.Kind == SpaceKind.ChannelPublic || s.Kind == SpaceKind.ChannelPrivate)
            && s.Id != exceptChannelId
            && s.Name.ToLower() == key, ct);

        if (clash)
            throw new ConflictException("A channel with this name already exists in the Workspace.", ChatRuleCodes.NameConflict);
    }

    /// <summary>Group names are unique per Workspace, ignoring case.</summary>
    public static async Task RequireUniqueGroupNameAsync(IChatDbContext db, Guid workspaceId, string name, Guid? exceptGroupId, CancellationToken ct)
    {
        var key = name.Trim().ToLowerInvariant();
        var clash = await db.ChatChannelGroups.AnyAsync(g =>
            g.WorkspaceId == workspaceId && g.Id != exceptGroupId && g.Name.ToLower() == key, ct);

        if (clash)
            throw new ConflictException("A channel group with this name already exists in the Workspace.", ChatRuleCodes.NameConflict);
    }

    public static async Task RequireGroupAsync(IChatDbContext db, Guid workspaceId, Guid groupId, CancellationToken ct)
    {
        var exists = await db.ChatChannelGroups.AnyAsync(g => g.Id == groupId && g.WorkspaceId == workspaceId, ct);
        if (!exists)
            throw new NotFoundException(nameof(ChannelGroup), groupId);
    }

    public static async Task<double> NextChannelPositionAsync(IChatDbContext db, Guid workspaceId, CancellationToken ct)
    {
        var positions = await db.ChatSpaces
            .Where(s => s.WorkspaceId == workspaceId && s.DeletedAt == null)
            .Select(s => s.Position)
            .ToListAsync(ct);
        return positions.Count == 0 ? 1 : positions.Max() + 1;
    }

    public static async Task<double> NextGroupPositionAsync(IChatDbContext db, Guid workspaceId, CancellationToken ct)
    {
        var positions = await db.ChatChannelGroups
            .Where(g => g.WorkspaceId == workspaceId)
            .Select(g => g.Position)
            .ToListAsync(ct);
        return positions.Count == 0 ? 1 : positions.Max() + 1;
    }
}
