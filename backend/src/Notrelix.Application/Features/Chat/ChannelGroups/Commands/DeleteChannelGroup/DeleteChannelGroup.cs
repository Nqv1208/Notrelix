using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.ChannelGroups.Commands.DeleteChannelGroup;

/// <summary>
/// Deletes a category. Its channels are not deleted: they move out of the category and keep their place.
/// A stale expected version is 412.
/// </summary>
public record DeleteChannelGroupCommand(Guid WorkspaceId, Guid GroupId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatGroupManage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.channel-group"), GroupId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class DeleteChannelGroupCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<DeleteChannelGroupCommand, Result>
{
    public async Task<Result> Handle(DeleteChannelGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await db.ChatChannelGroups.FirstOrDefaultAsync(
            g => g.Id == request.GroupId && g.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChannelGroup), request.GroupId);

        var now = clock.UtcNow;
        var userId = requestContext.UserId;
        var inGroup = await db.ChatSpaces
            .Where(s => s.WorkspaceId == request.WorkspaceId && s.GroupId == request.GroupId && s.DeletedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var space in inGroup)
            space.RemoveFromGroup(userId, now);

        db.ChatChannelGroups.Remove(group);
        return Result.Success();
    }
}

public class DeleteChannelGroupCommandValidator : AbstractValidator<DeleteChannelGroupCommand>
{
    public DeleteChannelGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
