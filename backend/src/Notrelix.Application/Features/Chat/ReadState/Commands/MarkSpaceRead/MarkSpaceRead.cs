using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Time;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.ReadState.DTOs;
using Notrelix.Application.Features.Chat.ReadState.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.ReadStates;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.ReadState.Commands.MarkSpaceRead;

/// <summary>
/// Moves the caller's read boundary in a space forward to the sequence they have seen (UC-CHT-D02). A lower or equal
/// sequence changes nothing, and a sequence above the space's last one reads the space to its end. A space the caller is
/// not an active member of is the same NotFound as a missing one.
/// </summary>
public record MarkSpaceReadCommand(Guid WorkspaceId, Guid SpaceId, long UpToSequence)
    : ICommand<Result<ReadBoundaryDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class MarkSpaceReadCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatReadStateStore readStates,
    IIntegrationEventCollector events,
    IDateTimeProvider clock) : IRequestHandler<MarkSpaceReadCommand, Result<ReadBoundaryDto>>
{
    public async Task<Result<ReadBoundaryDto>> Handle(MarkSpaceReadCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        // Bounded by the last sequence read here. The last sequence only grows, so the target never passes a stored message.
        var target = ReadBoundary.Clamp(request.UpToSequence, space.LastSequence);
        if (!await readStates.AdvanceAsync(request.WorkspaceId, space.Id, userId, target, cancellationToken))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var readSequence = await db.ChatMemberships.AsNoTracking()
            .Where(m => m.SpaceId == space.Id && m.UserId == userId)
            .Select(m => m.ReadSequence)
            .FirstAsync(cancellationToken);

        // The person's other devices learn the new count of this space (UC-CHT-C09).
        var summary = await UnreadSummaryBuilder.BuildAsync(db, request.WorkspaceId, userId, clock.UtcNow, cancellationToken);
        ReadStateRealtime.Announce(events, requestContext.RequireAccountId(), request.WorkspaceId, userId,
            summary.Spaces.Where(s => s.SpaceId == space.Id), clock.UtcNow);

        return Result<ReadBoundaryDto>.Success(new ReadBoundaryDto(space.Id, readSequence, space.LastSequence));
    }
}

public class MarkSpaceReadCommandValidator : AbstractValidator<MarkSpaceReadCommand>
{
    public MarkSpaceReadCommandValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.UpToSequence).GreaterThanOrEqualTo(0).WithMessage("upToSequence is 0 or more.");
    }
}
