using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.ReadState.DTOs;
using Notrelix.Application.Features.Chat.ReadState.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Features.Workspaces.Public.Lifecycle;
using Notrelix.Domain.Chat.ReadStates;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.ReadState.Commands.MarkSpaceUnreadFrom;

/// <summary>
/// Mark unread from here (UC-CHT-I07): moves the caller's own read boundary back to just before the message at
/// <see cref="FromSequence"/>, so that message and every later one count as unread again. It is the one command that lowers a
/// boundary; it never moves it forward (a message already unread changes nothing) and it touches no one else's state. A space the
/// caller is not an active member of is the same NotFound as a missing one.
/// </summary>
public record MarkSpaceUnreadFromCommand(Guid WorkspaceId, Guid SpaceId, long FromSequence)
    : ICommand<Result<ReadBoundaryDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IPersonalWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class MarkSpaceUnreadFromCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatReadStateStore readStates,
    IIntegrationEventCollector events,
    IDateTimeProvider clock) : IRequestHandler<MarkSpaceUnreadFromCommand, Result<ReadBoundaryDto>>
{
    public async Task<Result<ReadBoundaryDto>> Handle(MarkSpaceUnreadFromCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        // A sequence past the last one names no message: nothing becomes unread.
        if (request.FromSequence <= space.LastSequence
            && !await readStates.RewindAsync(request.WorkspaceId, space.Id, userId, ReadBoundary.Before(request.FromSequence), cancellationToken))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var readSequence = await db.ChatMemberships.AsNoTracking()
            .Where(m => m.SpaceId == space.Id && m.UserId == userId)
            .Select(m => m.ReadSequence)
            .FirstAsync(cancellationToken);

        // The person's other devices learn the new count of this space (UC-CHT-C09).
        var summary = await UnreadSummaryBuilder.BuildAsync(db, request.WorkspaceId, userId, clock.UtcNow, cancellationToken, space.Id);
        ReadStateRealtime.Announce(events, requestContext.RequireAccountId(), request.WorkspaceId, userId,
            summary.Spaces.Where(s => s.SpaceId == space.Id), clock.UtcNow);

        return Result<ReadBoundaryDto>.Success(new ReadBoundaryDto(space.Id, readSequence, space.LastSequence));
    }
}

public class MarkSpaceUnreadFromCommandValidator : AbstractValidator<MarkSpaceUnreadFromCommand>
{
    public MarkSpaceUnreadFromCommandValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.FromSequence).GreaterThanOrEqualTo(1).WithMessage("fromSequence is 1 or more.");
    }
}
