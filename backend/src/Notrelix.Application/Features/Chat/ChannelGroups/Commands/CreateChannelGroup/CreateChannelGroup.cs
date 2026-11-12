using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.ChannelGroups.Commands.CreateChannelGroup;

/// <summary>Creates a sidebar category (UC-CHT-A02). Names are unique per Workspace, ignoring case.</summary>
[IdempotencyOperation("chat.channel-groups.create-channel-group.v1")]
public record CreateChannelGroupCommand(Guid WorkspaceId, string Name, double? Position)
    : ICommand<Result<ChannelGroupDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ChatGroupManage;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CreateChannelGroupCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<CreateChannelGroupCommand, Result<ChannelGroupDto>>
{
    public async Task<Result<ChannelGroupDto>> Handle(CreateChannelGroupCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        var name = request.Name.Trim();

        await ChannelAccess.RequireUniqueGroupNameAsync(db, request.WorkspaceId, name, null, cancellationToken);
        var position = request.Position ?? await ChannelAccess.NextGroupPositionAsync(db, request.WorkspaceId, cancellationToken);

        var group = ChannelGroup.Create(accountId, request.WorkspaceId, name, position, userId, clock.UtcNow);
        db.ChatChannelGroups.Add(group);
        return Result<ChannelGroupDto>.Success(ChannelGroupDto.From(group));
    }
}

public class CreateChannelGroupCommandValidator : AbstractValidator<CreateChannelGroupCommand>
{
    public CreateChannelGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ChatLimits.ChannelGroupNameMaxLength);
        RuleFor(x => x.Position).Must(p => p is null || double.IsFinite(p.Value))
            .WithMessage("Position must be a finite number.");
    }
}
