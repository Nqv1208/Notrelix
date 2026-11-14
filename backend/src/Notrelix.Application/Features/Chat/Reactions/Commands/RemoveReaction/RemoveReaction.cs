using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Reactions.DTOs;
using Notrelix.Application.Features.Chat.Reactions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;
using Reaction = Notrelix.Domain.Chat.Messages.Reaction;

namespace Notrelix.Application.Features.Chat.Reactions.Commands.RemoveReaction;

/// <summary>
/// Removes the caller's own reaction of one kind from a message (UC-CHT-G07). Only an active member of the space removes, under the
/// react action; a kind off the palette is 400. The request is idempotent: a reaction the caller does not have is a success (200)
/// that changes nothing. A message the caller cannot see is the same NotFound as a missing one. An archived channel is 409. The
/// response is the message's reaction summary after the change.
/// </summary>
public record RemoveReactionCommand(Guid WorkspaceId, Guid MessageId, string Kind)
    : ICommand<Result<MessageReactionsDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMessageReact;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class RemoveReactionCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IDateTimeProvider clock) : IRequestHandler<RemoveReactionCommand, Result<MessageReactionsDto>>
{
    public async Task<Result<MessageReactionsDto>> Handle(RemoveReactionCommand request, CancellationToken cancellationToken)
    {
        Reaction.EnsureKind(request.Kind);
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

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

        var summary = await ReactionSummaries.ForMessageAsync(db, request.WorkspaceId, userId, message.Id, cancellationToken);

        // Tracked on purpose: the reaction is removed and its end is raised as a fact. A missing one changes nothing.
        var reaction = await db.ChatReactions.FirstOrDefaultAsync(
            r => r.MessageId == message.Id && r.UserId == userId && r.Kind == request.Kind, cancellationToken);
        if (reaction is null)
            return Result<MessageReactionsDto>.Success(new MessageReactionsDto(message.Id, summary));

        reaction.Remove(userId, clock.UtcNow);
        db.ChatReactions.Remove(reaction);

        var after = ReactionSummaries.Apply(summary, request.Kind, added: false);
        return Result<MessageReactionsDto>.Success(new MessageReactionsDto(message.Id, after));
    }
}

public class RemoveReactionCommandValidator : AbstractValidator<RemoveReactionCommand>
{
    public RemoveReactionCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Kind)
            .Must(ReactionKinds.Contains)
            .WithMessage("The emoji is not in the reaction palette.");
    }
}
