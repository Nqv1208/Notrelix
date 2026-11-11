using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.ChannelGroups.Commands.UpdateChannelGroup;

/// <summary>Renames a category or moves it in the sidebar (UC-CHT-A02, A04). A stale expected version is 412.</summary>
public record UpdateChannelGroupCommand(Guid WorkspaceId, Guid GroupId, string Name, double Position, long ExpectedVersion)
    : ICommand<Result<ChannelGroupDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatGroupManage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.channel-group"), GroupId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class UpdateChannelGroupCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<UpdateChannelGroupCommand, Result<ChannelGroupDto>>
{
    public async Task<Result<ChannelGroupDto>> Handle(UpdateChannelGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await db.ChatChannelGroups.FirstOrDefaultAsync(
            g => g.Id == request.GroupId && g.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChannelGroup), request.GroupId);

        await ChannelAccess.RequireUniqueGroupNameAsync(db, request.WorkspaceId, request.Name, group.Id, cancellationToken);
        group.Update(request.Name, request.Position, requestContext.UserId, clock.UtcNow);

        return Result<ChannelGroupDto>.Success(ChannelGroupDto.From(group));
    }
}

public class UpdateChannelGroupCommandValidator : AbstractValidator<UpdateChannelGroupCommand>
{
    public UpdateChannelGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ChatLimits.ChannelGroupNameMaxLength);
        RuleFor(x => x.Position).Must(double.IsFinite).WithMessage("Position must be a finite number.");
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
