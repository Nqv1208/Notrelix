using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Channels.Commands.ReorderChannels;

/// <summary>
/// Sets the shared sidebar order of channels (UC-CHT-A04). Each channel moved needs the manage right on it. A stale
/// version of any channel refuses the whole batch with 412 and changes nothing.
/// </summary>
public record ReorderChannelsCommand(Guid WorkspaceId, IReadOnlyList<ChannelPlacement> Placements)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatChannelManage;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ReorderChannelsCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<ReorderChannelsCommand, Result>
{
    public async Task<Result> Handle(ReorderChannelsCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var now = clock.UtcNow;

        // Every channel is checked before any is moved, so a refusal changes nothing.
        var loaded = new List<(Space Space, ChannelPlacement Placement)>();
        foreach (var placement in request.Placements)
        {
            var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, placement.ChannelId, cancellationToken);
            ChannelAccess.RequireVisible(space, caller, userId);
            ChannelAccess.RequireManager(space, caller, userId);
            if (space.Version != placement.ExpectedVersion)
                throw new PreconditionFailedException(
                    "The channel changed since it was read. Reload the sidebar and try again.",
                    expectedVersion: placement.ExpectedVersion,
                    currentVersion: space.Version);
            if (placement.GroupId is { } groupId)
                await ChannelAccess.RequireGroupAsync(db, request.WorkspaceId, groupId, cancellationToken);
            loaded.Add((space, placement));
        }

        foreach (var (space, placement) in loaded)
            space.MoveTo(placement.GroupId, placement.Position, userId, now);

        return Result.Success();
    }
}

public class ReorderChannelsCommandValidator : AbstractValidator<ReorderChannelsCommand>
{
    public ReorderChannelsCommandValidator()
    {
        RuleFor(x => x.Placements).NotEmpty().Must(p => p.Count <= 200)
            .WithMessage("At most 200 channels can be reordered at once.");
        RuleForEach(x => x.Placements).ChildRules(p =>
        {
            p.RuleFor(x => x.ChannelId).NotEmpty();
            p.RuleFor(x => x.Position).Must(double.IsFinite).WithMessage("Position must be a finite number.");
            p.RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        });
    }
}
