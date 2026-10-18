using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.AddSpaceMember;

/// <summary>
/// Adds a User (an active member of the same Workspace) or a Team (an active
/// Team of the same Workspace) to a Space with a Space role (WSP-024).
/// Authorized as <c>ManageSpaces</c> on the Space; guarded by its expected
/// version. Re-adding the same subject with the same role is a no-op.
/// </summary>
public record AddSpaceMemberCommand(
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

public class AddSpaceMemberCommandHandler : IRequestHandler<AddSpaceMemberCommand, Result>
{
    private readonly ISpaceStore _spaces;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddSpaceMemberCommandHandler(ISpaceStore spaces, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _spaces = spaces;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AddSpaceMemberCommand request, CancellationToken ct)
    {
        var space = await _spaces.FindAsync(request.WorkspaceId, request.SpaceId, ct)
            ?? throw new NotFoundException(nameof(Space), request.SpaceId);

        var subjectType = Enum.Parse<SpaceMemberSubjectType>(request.SubjectType, ignoreCase: true);
        var role = Enum.Parse<SpaceRole>(request.Role, ignoreCase: true);

        // WSP-014 / WSP-024: subjects come from the same Workspace only; a raw id
        // from another Workspace (or a removed member / archived Team) is not a subject.
        var eligible = subjectType == SpaceMemberSubjectType.User
            ? await _spaces.IsActiveWorkspaceMemberAsync(request.WorkspaceId, request.SubjectId, ct)
            : await _spaces.IsActiveTeamAsync(request.WorkspaceId, request.SubjectId, ct);
        if (!eligible)
            throw new NotFoundException(subjectType == SpaceMemberSubjectType.User ? "WorkspaceMember" : nameof(Team), request.SubjectId);

        space.AddMember(subjectType, request.SubjectId, role, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
