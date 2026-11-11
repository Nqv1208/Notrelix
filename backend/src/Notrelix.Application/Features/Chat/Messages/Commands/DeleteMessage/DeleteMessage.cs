using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Messages.Commands.DeleteMessage;

/// <summary>
/// Deletes the caller's own message, leaving a tombstone (UC-CHT-B05). The content goes; the author, the time and the
/// sequence stay, so a thread keeps its place. Deleting a message that is already a tombstone changes nothing.
/// A deleted message loses its pin (UC-CHT-F06). The delete takes the space lock, as a pin does, so a pin and a delete of one
/// message cannot both commit.
/// </summary>
public record DeleteMessageCommand(Guid WorkspaceId, Guid MessageId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.message"), MessageId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class DeleteMessageCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IDateTimeProvider clock) : IRequestHandler<DeleteMessageCommand, Result>
{
    public async Task<Result> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        // Tracked on purpose: the data session binds the expected version to this loaded aggregate.
        var message = await db.ChatMessages.FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        if (message.AuthorId != userId)
            throw new ForbiddenException("Only the author can delete this message.");
        if (space.IsArchived && !message.IsDeleted)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        if (message.IsDeleted)
            return Result.Success();

        await sequences.LockAsync(space.Id, cancellationToken);

        var now = clock.UtcNow;
        if (message.Delete(userId, now))
        {
            var pins = await db.ChatPins.Where(p => p.MessageId == message.Id).ToListAsync(cancellationToken);
            foreach (var pin in pins)
            {
                pin.Remove(userId, now);
                db.ChatPins.Remove(pin);
            }
        }
        return Result.Success();
    }
}

public class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
