using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Reactions.DTOs;
using Notrelix.Application.Features.Chat.Reactions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Permissions;

using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;
using Reaction = Notrelix.Domain.Chat.Messages.Reaction;

namespace Notrelix.Application.Features.Chat.Reactions.Commands.AddReaction;

/// <summary>
/// Adds the caller's reaction of one palette kind to a message (UC-CHT-G06, CHT-011, COL-009). Only an active member of the space
/// reacts, under the react action; a Guest is refused by the action. A kind off the palette is 400. A system message or a deleted
/// message is 409. An archived channel is 409. A reaction the caller already has is a no-op (200) that changes nothing; a new one
/// is 201. The read of the reaction set and the insert run under the space lock, so two requests for one reaction cannot both
/// insert it. The response is the message's reaction summary after the change.
/// </summary>
public record AddReactionCommand(Guid WorkspaceId, Guid MessageId, string Kind)
    : ICommand<Result<ReactionResultDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMessageReact;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class AddReactionCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IDateTimeProvider clock) : IRequestHandler<AddReactionCommand, Result<ReactionResultDto>>
{
    public async Task<Result<ReactionResultDto>> Handle(AddReactionCommand request, CancellationToken cancellationToken)
    {
        Reaction.EnsureKind(request.Kind);
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
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

        // Standing, the archive state and the message are read again after the lock: a leave, an archive or a delete that committed
        // while this request waited is respected.
        var live = await db.ChatSpaces.AsNoTracking()
            .Where(s => s.Id == space.Id && s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null)
            .Select(s => new { Archived = s.ArchivedAt != null })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Space), message.SpaceId);
        var stillMember = await db.ChatMemberships.AsNoTracking()
            .AnyAsync(m => m.SpaceId == space.Id && m.UserId == userId && m.LeftAt == null, cancellationToken);
        if (!stillMember)
            throw new NotFoundException(nameof(Space), message.SpaceId);
        if (live.Archived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);
        await ChannelPermissionChecks.RequireAsync(db, space, caller, userId, ChannelAction.React, cancellationToken);

        var current = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);
        current.EnsureReactable();

        var summary = await ReactionSummaries.ForMessageAsync(db, request.WorkspaceId, userId, current.Id, cancellationToken);
        var alreadyHas = await db.ChatReactions.AsNoTracking().AnyAsync(
            r => r.MessageId == current.Id && r.UserId == userId && r.Kind == request.Kind, cancellationToken);
        if (alreadyHas)
            return Result<ReactionResultDto>.Success(new ReactionResultDto(new MessageReactionsDto(current.Id, summary), Created: false));

        var distinctKinds = await db.ChatReactions.AsNoTracking()
            .Where(r => r.MessageId == current.Id)
            .Select(r => r.Kind)
            .Distinct()
            .CountAsync(cancellationToken);
        var kindIsNew = !summary.Any(r => r.Emoji == request.Kind);
        var byActor = await db.ChatReactions.AsNoTracking()
            .CountAsync(r => r.MessageId == current.Id && r.UserId == userId, cancellationToken);
        Reaction.EnsureRoom(distinctKinds, byActor, kindIsNew);

        var now = clock.UtcNow;
        var reaction = Reaction.Create(accountId, request.WorkspaceId, space.Id, current.Id, userId, request.Kind, now);
        db.ChatReactions.Add(reaction);

        var after = ReactionSummaries.Apply(summary, request.Kind, added: true);
        return Result<ReactionResultDto>.Success(new ReactionResultDto(new MessageReactionsDto(current.Id, after), Created: true));
    }
}

public class AddReactionCommandValidator : AbstractValidator<AddReactionCommand>
{
    public AddReactionCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Kind)
            .Must(ReactionKinds.Contains)
            .WithMessage("The emoji is not in the reaction palette.");
    }
}
