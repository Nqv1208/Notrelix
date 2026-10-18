using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceMemberRole;

/// <summary>
/// Changes the Space role of a User or Team entry (WSP-024). The last Admin
/// entry cannot be downgraded. Authorized as <c>ManageSpaces</c> on the Space;
/// guarded by its expected version.
/// </summary>
public record ChangeSpaceMemberRoleCommand(
    Guid WorkspaceId,
    Guid SpaceId,
    string SubjectType,
    Guid SubjectId,
    string Role,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageSpaces;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.space"), SpaceId, WorkspaceId);
}

public class ChangeSpaceMemberRoleCommandHandler : IRequestHandler<ChangeSpaceMemberRoleCommand, Result>
{
    private readonly ISpaceStore _spaces;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeSpaceMemberRoleCommandHandler(ISpaceStore spaces, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _spaces = spaces;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ChangeSpaceMemberRoleCommand request, CancellationToken ct)
    {
        var space = await _spaces.FindAsync(request.WorkspaceId, request.SpaceId, ct)
            ?? throw new NotFoundException(nameof(Space), request.SpaceId);

        var subjectType = Enum.Parse<SpaceMemberSubjectType>(request.SubjectType, ignoreCase: true);
        var role = Enum.Parse<SpaceRole>(request.Role, ignoreCase: true);
        space.ChangeMemberRole(subjectType, request.SubjectId, role, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
