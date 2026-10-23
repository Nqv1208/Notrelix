using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberJobTitle;

/// <summary>
/// Sets or clears another member's job title in this Workspace (D15). Workspace Owner and Admin
/// hold this capability by default (<c>ManageMemberProfile</c> on the Workspace); the caller's own
/// title has its own command. Guarded by the target member's expected version.
/// </summary>
public record UpdateMemberJobTitleCommand(
    Guid WorkspaceId,
    Guid UserId,
    string? JobTitle,
    long ExpectedVersion
) : ICommand<Result<WorkspaceMemberProfileDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageMemberProfile;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    // The concurrency authority is the WorkspaceMember aggregate; authorization stays on the Workspace.
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace-member"), UserId, WorkspaceId);
}

public class UpdateMemberJobTitleCommandHandler : IRequestHandler<UpdateMemberJobTitleCommand, Result<WorkspaceMemberProfileDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateMemberJobTitleCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<WorkspaceMemberProfileDto>> Handle(UpdateMemberJobTitleCommand request, CancellationToken ct)
    {
        var profile = await WorkspaceMemberJobTitleWriter.ApplyAsync(
            _context, request.WorkspaceId, request.UserId, request.JobTitle, _requestContext.UserId, _dateTimeProvider.UtcNow, ct);
        return Result<WorkspaceMemberProfileDto>.Success(profile);
    }
}
