using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.RemoveSpaceMember;

/// <summary>
/// Removes a User or Team entry from a Space (WSP-024). The last Admin entry
/// cannot be removed. Authorized as <c>ManageSpaces</c> on the Space; guarded by
/// its expected version. Removing an absent subject is a no-op.
/// </summary>
public record RemoveSpaceMemberCommand(
    Guid WorkspaceId,
    Guid SpaceId,
    string SubjectType,
    Guid SubjectId,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageSpaces;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.space"), SpaceId, WorkspaceId);
}

public class RemoveSpaceMemberCommandHandler : IRequestHandler<RemoveSpaceMemberCommand, Result>
{
    private readonly ISpaceStore _spaces;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IAccessChangeSignal _access;

    public RemoveSpaceMemberCommandHandler(ISpaceStore spaces, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IAccessChangeSignal access)
    {
        _access = access;
        _spaces = spaces;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RemoveSpaceMemberCommand request, CancellationToken ct)
    {
        var space = await _spaces.FindAsync(request.WorkspaceId, request.SpaceId, ct)
            ?? throw new NotFoundException(nameof(Space), request.SpaceId);

        var subjectType = Enum.Parse<SpaceMemberSubjectType>(request.SubjectType, ignoreCase: true);
        space.RemoveMember(subjectType, request.SubjectId, _requestContext.UserId, _dateTimeProvider.UtcNow);
        if (string.Equals(request.SubjectType, "User", StringComparison.OrdinalIgnoreCase)) _access.WorkspacePerson(request.WorkspaceId, request.SubjectId); else _access.Workspace(request.WorkspaceId);
        return Result.Success();
    }
}
