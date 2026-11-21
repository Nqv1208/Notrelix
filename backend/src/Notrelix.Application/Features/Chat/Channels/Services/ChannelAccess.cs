using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
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

    public static bool IsGuest(WorkspaceMemberFact caller) => string.Equals(caller.Role, GuestRole, StringComparison.Ordinal);

    /// <summary>
    /// The caller's standing in the Workspace. The pipeline already admitted them, so a missing fact is a missing channel. A member who
    /// is not active (suspended, a deactivated Guest included) has no access either: the same NotFound, so access ends at the next call
    /// even when a gateway or a job reaches a handler without the pipeline's check (CHT-004).
    /// </summary>
    public static async Task<WorkspaceMemberFact> CallerAsync(
        IWorkspaceMemberFacts members, Guid workspaceId, Guid userId, CancellationToken ct)
    {
        var fact = await members.FindAsync(workspaceId, userId, ct);
        return fact is { IsActive: true } ? fact : throw new NotFoundException(nameof(Space), workspaceId);
    }

    /// <summary>A live channel of the Workspace with its memberships loaded, or the same NotFound as a missing one.</summary>
    public static async Task<Space> LoadLiveAsync(IChatDbContext db, Guid workspaceId, Guid channelId, CancellationToken ct)
    {
        var space = await db.ChatSpaces.FirstOrDefaultAsync(
            s => s.Id == channelId && s.WorkspaceId == workspaceId && s.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Space), channelId);

        // Loading the rows through the DbSet lets EF fix up the aggregate's membership collection.
        await db.ChatMemberships.Where(m => m.SpaceId == channelId).ToListAsync(ct);
        await db.ChatPermissionOverrides.Where(o => o.SpaceId == channelId).ToListAsync(ct);
        return space;
    }

    /// <summary>A deleted channel of the Workspace (never a conversation) with its memberships loaded, or the same NotFound as a missing one.</summary>
    public static async Task<Space> LoadDeletedChannelAsync(IChatDbContext db, Guid accountId, Guid workspaceId, Guid channelId, CancellationToken ct)
    {
        // The context hides soft-deleted rows by default, and bypassing its filter also drops its tenant predicate (EF Core 9),
        // so the account and the Workspace are applied here.
        var space = await db.ChatSpaces.IgnoreQueryFilters().FirstOrDefaultAsync(
            s => s.Id == channelId && s.AccountId == accountId && s.WorkspaceId == workspaceId && s.DeletedAt != null, ct)
            ?? throw new NotFoundException(nameof(Space), channelId);
        if (space.IsConversation)
            throw new NotFoundException(nameof(Space), channelId);

        await db.ChatMemberships.Where(m => m.SpaceId == channelId).ToListAsync(ct);
        await db.ChatPermissionOverrides.Where(o => o.SpaceId == channelId).ToListAsync(ct);
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
        if (!CanSee(space, caller, userId))
            throw new NotFoundException(nameof(Space), space.Id);
    }

    /// <summary>The rule <see cref="RequireVisible"/> enforces, as a yes or no.</summary>
    public static bool CanSee(Space space, WorkspaceMemberFact caller, Guid userId)
    {
        var mustBeMember = !space.IsPublic || string.Equals(caller.Role, GuestRole, StringComparison.Ordinal);
        return !mustBeMember || space.IsActiveMember(userId);
    }

    /// <summary>
    /// The earliest moment a person may read in a space: the time they joined a group conversation, so a person added later sees the
    /// history from their join point only (UC-CHT-E03). Null for a channel and for a direct conversation, whose history is whole.
    /// </summary>
    public static DateTimeOffset? HistoryFloor(Space space, Guid userId) =>
        space.Kind == SpaceKind.Group ? space.GetActiveMembership(userId)?.JoinedAt : null;

    /// <summary>A message older than the caller's <see cref="HistoryFloor"/> is the same NotFound as a missing one, so its existence is not revealed.</summary>
    public static void RequireInHistory(Space space, Guid userId, Message message)
    {
        if (HistoryFloor(space, userId) is { } floor && message.CreatedAt < floor)
            throw new NotFoundException(nameof(Message), message.Id);
    }

    /// <summary>
    /// The SQL twin of <see cref="RequireVisible"/>: the live spaces of the Workspace the caller may see now. A public channel is
    /// seen by every non-guest member, anything else (a private channel, a conversation, and a public channel for a Guest) by an
    /// active member only. Owner and Admin get nothing by role (CHT-004). Search reads through this so it cannot drift from the
    /// history handlers; a test runs both over the same matrix.
    /// </summary>
    public static IQueryable<Space> VisibleSpaces(IChatDbContext db, Guid workspaceId, Guid userId, bool isGuest)
    {
        var active = db.ChatMemberships.Where(m => m.UserId == userId && m.LeftAt == null).Select(m => m.SpaceId);
        return db.ChatSpaces.Where(s =>
            s.WorkspaceId == workspaceId
            && s.DeletedAt == null
            && ((!isGuest && s.Kind == SpaceKind.ChannelPublic) || active.Contains(s.Id)));
    }

    /// <summary>Renaming, archiving, moving and removing others belong to a channel manager, a Workspace Owner or Admin.</summary>
    public static void RequireManager(Space space, WorkspaceMemberFact caller, Guid userId)
    {
        if (!CanManage(space, caller, userId))
            throw new ForbiddenException("Only a channel manager, a Workspace Owner or Admin can change this channel.");
    }

    /// <summary>The rule <see cref="RequireManager"/> enforces, as a yes or no. A Guest never manages a channel, whatever role a row holds.</summary>
    public static bool CanManage(Space space, WorkspaceMemberFact caller, Guid userId) =>
        (!IsGuest(caller) && space.IsActiveManager(userId)) || caller.Role is "Owner" or "Admin";

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
