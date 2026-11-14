using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Permissions;

using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Pins.Commands.UnpinMessage;

/// <summary>
/// Removes the pin of a message (UC-CHT-F07). Only an active member of the space unpins, under the pin action. The request is
/// idempotent: a message that is not pinned, or a pin already gone, is a success (204) that changes nothing. A message the
/// caller cannot see is the same NotFound as a missing one. An archived channel is refused (409).
/// </summary>
public record UnpinMessageCommand(Guid WorkspaceId, Guid MessageId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMessagePin;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class UnpinMessageCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IDateTimeProvider clock) : IRequestHandler<UnpinMessageCommand, Result>
{
    public async Task<Result> Handle(UnpinMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var message = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), message.SpaceId);
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        await sequences.LockAsync(space.Id, cancellationToken);

        var stillMember = await db.ChatMemberships.AsNoTracking()
            .AnyAsync(m => m.SpaceId == space.Id && m.UserId == userId && m.LeftAt == null, cancellationToken);
        if (!stillMember)
            throw new NotFoundException(nameof(Space), message.SpaceId);
        var archived = await db.ChatSpaces.AsNoTracking()
            .Where(s => s.Id == space.Id && s.DeletedAt == null)
            .Select(s => s.ArchivedAt != null)
            .FirstOrDefaultAsync(cancellationToken);
        if (archived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);
        await ChannelPermissionChecks.RequireAsync(db, space, caller, userId, ChannelAction.Pin, cancellationToken);

        // Tracked on purpose: the pin is removed and its end is raised as a fact.
        var pin = await db.ChatPins.FirstOrDefaultAsync(p => p.MessageId == request.MessageId, cancellationToken);
        if (pin is null)
            return Result.Success();

        pin.Remove(userId, clock.UtcNow);
        db.ChatPins.Remove(pin);
        return Result.Success();
    }
}

public class UnpinMessageCommandValidator : AbstractValidator<UnpinMessageCommand>
{
    public UnpinMessageCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
    }
}
