using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Rules;
using Microsoft.Extensions.Options;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Calls.Services;

/// <summary>
/// The access rules of a call, one place for every command and query. A call is a fact of its space, so its readers and actors are
/// the active members of that space and nobody else: an outsider, a person who left, and a call of another Workspace all answer
/// NotFound, the same as a missing call.
/// </summary>
public static class ChatCallAccess
{
    public static void RequireEnabled(IOptions<ChatCallOptions> options)
    {
        if (!options.Value.Enabled)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(ChatRuleCodes.CallsUnavailable, "Calls are switched off.");
    }

    /// <summary>The space of the call, loaded with its members, for an active member; NotFound for anyone else.</summary>
    public static async Task<(Space Space, WorkspaceMemberFact Caller)> MemberSpaceAsync(
        IChatDbContext db, IWorkspaceMemberFacts members, Guid workspaceId, Guid spaceId, Guid userId, CancellationToken ct)
    {
        var caller = await ChannelAccess.CallerAsync(members, workspaceId, userId, ct);
        var space = await ChannelAccess.LoadLiveAsync(db, workspaceId, spaceId, ct);
        ChannelAccess.RequireVisible(space, caller, userId);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), spaceId);
        return (space, caller);
    }

    /// <summary>The tracked call with its participants, for an active member of its space.</summary>
    public static async Task<(Call Call, Space Space, WorkspaceMemberFact Caller)> MemberCallAsync(
        IChatDbContext db, IWorkspaceMemberFacts members, Guid workspaceId, Guid callId, Guid userId, CancellationToken ct)
    {
        var call = await db.ChatCalls.Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == callId && c.WorkspaceId == workspaceId, ct)
            ?? throw new NotFoundException(nameof(Call), callId);
        var (space, caller) = await MemberSpaceAsync(db, members, workspaceId, call.SpaceId, userId, ct);
        return (call, space, caller);
    }

    /// <summary>Whether the person holds a live participation in a call other than <paramref name="exceptCallId"/> (invariant 2).</summary>
    public static Task<bool> IsBusyAsync(IChatDbContext db, Guid personId, Guid? exceptCallId, CancellationToken ct) =>
        db.ChatCallParticipants.AsNoTracking()
            .AnyAsync(p => p.PersonId == personId && p.LeftAt == null && (exceptCallId == null || p.CallId != exceptCallId), ct);

    public static ConflictException Busy() => new("You are already in another call.", ChatRuleCodes.CallBusy);

    public static CallJoinTokenDto ToDto(CallJoinToken token) => new(token.Url, token.Token, token.ExpiresAt);
}
