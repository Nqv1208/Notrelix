using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Messages.Commands.ModerateDeleteMessage;

/// <summary>
/// Removes another person's message as a moderator, with a reason (UC-CHT-H03). Needs the moderate action (a Workspace Owner or Admin
/// by default, or a custom role that holds it) and a channel the caller can see: a moderator does not read a private channel by role
/// (CHT-004), so it is the same NotFound as a missing one. The message becomes the same tombstone as an author's delete, with the moderator
/// kept as the one who removed it, so readers see "removed by a moderator". The reason and the actor go to the Audit fact, written once in
/// the same transaction; the message text is never copied anywhere. A message that is already a tombstone changes nothing. A system
/// message is 409 and the moderator's own message is 400 (delete it as its author). The author is not notified in this slice.
/// </summary>
public record ModerateDeleteMessageCommand(Guid WorkspaceId, Guid MessageId, string Reason, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessageModerate;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.message"), MessageId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ModerateDeleteMessageCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IDateTimeProvider clock,
    IChatMessageAttachments attachments,
    ChatAuditFacts audit) : IRequestHandler<ModerateDeleteMessageCommand, Result>
{
    public async Task<Result> Handle(ModerateDeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        // Tracked on purpose: the data session binds the expected version to this loaded aggregate.
        var message = await db.ChatMessages.FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        if (message.IsDeleted)
            return Result.Success();
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        await sequences.LockAsync(space.Id, cancellationToken);

        var now = clock.UtcNow;
        if (message.DeleteAsModerator(userId, now))
        {
            await MessageTombstones.ClearAttachedAsync(db, attachments, message, userId, now, cancellationToken);
            await audit.MessageModeratedAsync(space, message.Id, message.AuthorId, request.Reason.Trim(), cancellationToken);
        }
        return Result.Success();
    }
}

public class ModerateDeleteMessageCommandValidator : AbstractValidator<ModerateDeleteMessageCommand>
{
    public ModerateDeleteMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.Reason)
            .NotNull()
            .Must(reason => reason is not null && reason.Trim().Length is >= 1 and <= ChatLimits.ModerationReasonMaxLength)
            .WithMessage($"A reason of 1 to {ChatLimits.ModerationReasonMaxLength} characters is required.");
    }
}
