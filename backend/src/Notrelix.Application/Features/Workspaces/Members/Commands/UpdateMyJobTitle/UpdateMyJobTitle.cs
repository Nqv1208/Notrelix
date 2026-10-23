using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMyJobTitle;

/// <summary>
/// Sets or clears the caller's own job title in this Workspace (D15; a blank value clears it).
/// Any member of the Workspace may do it (<c>ViewWorkspace</c> on the Workspace); the subject is
/// always the principal. Guarded by the member's expected version, bound to the one WorkspaceMember
/// the handler loads, so the version resource is declared at the Workspace.
/// </summary>
public record UpdateMyJobTitleCommand(
    Guid WorkspaceId,
    string? JobTitle,
    long ExpectedVersion
) : ICommand<Result<WorkspaceMemberProfileDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace-member"), WorkspaceId, WorkspaceId);
}

public class UpdateMyJobTitleCommandHandler : IRequestHandler<UpdateMyJobTitleCommand, Result<WorkspaceMemberProfileDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateMyJobTitleCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<WorkspaceMemberProfileDto>> Handle(UpdateMyJobTitleCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var profile = await WorkspaceMemberJobTitleWriter.ApplyAsync(
            _context, request.WorkspaceId, userId, request.JobTitle, userId, _dateTimeProvider.UtcNow, ct);
        return Result<WorkspaceMemberProfileDto>.Success(profile);
    }
}
