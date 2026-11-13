using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Attachments.Services;

/// <summary>
/// The authorization of a write to a file of a message (attach, two-step create and commit): an active member of the space, the author of
/// the message, a live unarchived channel, a message that accepts attachments, and a channel that lets the caller attach. Shared by the
/// streaming upload and the two-step flow so both refuse the same things with the same codes.
/// </summary>
internal static class MessageAttachmentAccess
{
    public static async Task<(Message Message, Space Space, WorkspaceMemberFact Caller)> RequireAuthorAsync(
        IChatDbContext db,
        IWorkspaceMemberFacts members,
        Guid workspaceId,
        Guid messageId,
        Guid userId,
        bool tracked,
        CancellationToken cancellationToken)
    {
        var caller = await ChannelAccess.CallerAsync(members, workspaceId, userId, cancellationToken);

        // Tracked on purpose when the caller will change the message: the file fact then reaches the channel stream.
        var query = tracked ? db.ChatMessages : db.ChatMessages.AsNoTracking();
        var message = await query.FirstOrDefaultAsync(m => m.Id == messageId && m.WorkspaceId == workspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), messageId);

        var space = await ChannelAccess.LoadLiveAsync(db, workspaceId, message.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), message.SpaceId);
        if (message.AuthorId != userId)
            throw new ForbiddenException("Only the author can attach a file to this message.");
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);
        message.EnsureAttachable();
        await ChannelPermissionChecks.RequireAsync(db, space, caller, userId, ChannelAction.Attach, cancellationToken);

        return (message, space, caller);
    }
}
