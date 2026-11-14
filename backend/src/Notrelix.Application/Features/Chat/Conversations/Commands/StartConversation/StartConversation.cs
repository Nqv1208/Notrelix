using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Conversations.DTOs;
using Notrelix.Application.Features.Chat.Conversations.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Chat.Conversations.Commands.StartConversation;

/// <summary>
/// Starts a direct conversation with one other person, or a group with two or more others (UC-CHT-E01, E02). A direct conversation is
/// one per unordered pair of people in a Workspace: the start takes a lock for the pair, then either returns the conversation that
/// exists (200, created false) or stores the new one (201). Starting one with yourself is 400; a person who is not an active member
/// of the Workspace, a person blocked in either direction, or a Guest without a shared space is NotFound. A group holds at most
/// <see cref="ChatLimits.GroupCap"/> people counted with the caller; over it is 422 limit-reached, and people who cannot be added
/// are reported as skipped.
/// </summary>
[IdempotencyOperation("chat.conversations.start.v1")]
public record StartConversationCommand(Guid WorkspaceId, string Kind, Guid? UserId, IReadOnlyList<Guid>? UserIds, string? Name)
    : ICommand<Result<StartConversationResultDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class StartConversationCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDirectConversationLock pairLock,
    IDateTimeProvider clock) : IRequestHandler<StartConversationCommand, Result<StartConversationResultDto>>
{
    public async Task<Result<StartConversationResultDto>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        return request.Kind == SpaceWire.Direct
            ? await StartDirectAsync(request, userId, accountId, caller, cancellationToken)
            : await StartGroupAsync(request, userId, accountId, caller, cancellationToken);
    }

    private async Task<Result<StartConversationResultDto>> StartDirectAsync(
        StartConversationCommand request, Guid userId, Guid accountId, WorkspaceMemberFact caller, CancellationToken cancellationToken)
    {
        var other = request.UserId!.Value;
        if (other == userId)
            throw new DomainBusinessRuleException(ChatRuleCodes.SelfTarget, "A direct conversation needs two different people.");

        var target = await members.FindAsync(request.WorkspaceId, other, cancellationToken);
        if (target is not { IsActive: true })
            throw new NotFoundException(nameof(Space), other);
        if (await ConversationAccess.BlockedBetweenAsync(db, request.WorkspaceId, userId, other, cancellationToken))
            throw new NotFoundException(nameof(Space), other);
        if (!ConversationAccess.MayReach(caller, await ConversationAccess.SharesSpaceAsync(db, request.WorkspaceId, userId, other, cancellationToken)))
            throw new NotFoundException(nameof(Space), other);

        // The pair lock comes before the lookup: a second start of the same pair waits here, then finds the first one's row.
        var (low, high) = DirectPair.Order(userId, other);
        await pairLock.LockPairAsync(request.WorkspaceId, low, high, cancellationToken);

        var existingId = await db.ChatDirectPairs.AsNoTracking()
            .Where(p => p.WorkspaceId == request.WorkspaceId && p.UserLow == low && p.UserHigh == high)
            .Select(p => (Guid?)p.SpaceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId is { } spaceId)
        {
            var existing = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, spaceId, cancellationToken);
            // Starting a conversation again brings it back to the caller's list (the same as a new message).
            if (existing.GetActiveMembership(userId) is { IsHidden: true } mine)
                mine.Restore();
            return Result<StartConversationResultDto>.Success(new StartConversationResultDto(
                ConversationView(existing, userId), Created: false, Skipped: []));
        }

        var space = Space.CreateDirect(accountId, request.WorkspaceId, userId, other, clock.UtcNow);
        db.ChatSpaces.Add(space);
        db.ChatDirectPairs.Add(DirectPair.Create(accountId, request.WorkspaceId, userId, other, space.Id));
        return Result<StartConversationResultDto>.Success(new StartConversationResultDto(
            ConversationView(space, userId), Created: true, Skipped: []));
    }

    private async Task<Result<StartConversationResultDto>> StartGroupAsync(
        StartConversationCommand request, Guid userId, Guid accountId, WorkspaceMemberFact caller, CancellationToken cancellationToken)
    {
        var others = (request.UserIds ?? []).Where(id => id != userId).Distinct().ToList();
        if (others.Count + 1 > ChatLimits.GroupCap)
            throw new DomainBusinessRuleException(ChatRuleCodes.LimitReached, $"A group conversation holds at most {ChatLimits.GroupCap} people.");

        var added = new List<Guid>();
        var skipped = new List<Guid>();
        foreach (var candidate in others)
        {
            var fact = await members.FindAsync(request.WorkspaceId, candidate, cancellationToken);
            var reachable = fact is { IsActive: true }
                && ConversationAccess.MayReach(caller, await ConversationAccess.SharesSpaceAsync(db, request.WorkspaceId, userId, candidate, cancellationToken));
            if (reachable) added.Add(candidate);
            else skipped.Add(candidate);
        }

        var name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
        var space = Space.CreateGroup(accountId, request.WorkspaceId, name, [userId, .. added], userId, clock.UtcNow);
        db.ChatSpaces.Add(space);
        return Result<StartConversationResultDto>.Success(new StartConversationResultDto(
            ConversationView(space, userId), Created: true, Skipped: skipped));
    }

    /// <summary>The viewer's read model of a conversation the handler has just stored or loaded (no preview yet: it has no message).</summary>
    private ConversationDto ConversationView(Space space, Guid viewerId) =>
        ConversationDtoBuilder.From(space, space.GetActiveMembership(viewerId), unreadCount: 0, lastMessage: null);
}

public class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Kind).NotEmpty().Must(kind => kind is SpaceWire.Direct or SpaceWire.Group)
            .WithMessage("kind is direct or group.");
        When(x => x.Kind == SpaceWire.Direct, () =>
            RuleFor(x => x.UserId).NotNull().WithMessage("userId is required for a direct conversation."));
        When(x => x.Kind == SpaceWire.Group, () =>
        {
            RuleFor(x => x.UserIds).NotNull().Must(ids => ids is { Count: >= 2 })
                .WithMessage("A group conversation names at least two other people.");
            RuleForEach(x => x.UserIds).NotEmpty();
        });
        RuleFor(x => x.Name).MaximumLength(ChatLimits.ChannelNameMaxLength).When(x => x.Name is not null);
    }
}
