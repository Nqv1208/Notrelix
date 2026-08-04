using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSettings;

public record GetWorkspaceSettingsQuery(
    Guid WorkspaceId
) : IQuery<Result<WorkspaceSettingsDto>>, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public record WorkspaceSettingsDto(
    bool AllowPublicSharing,
    bool EnforceMfa,
    bool AllowGuestInvites,
    string DefaultMemberRole,
    int InvitationExpiryDays
);

public class GetWorkspaceSettingsQueryHandler : IRequestHandler<GetWorkspaceSettingsQuery, Result<WorkspaceSettingsDto>>
{
    private readonly IWorkspaceDbContext _context;

    public GetWorkspaceSettingsQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<WorkspaceSettingsDto>> Handle(GetWorkspaceSettingsQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var s = workspace.Settings;
        return Result<WorkspaceSettingsDto>.Success(new WorkspaceSettingsDto(
            s.AllowPublicSharing,
            s.EnforceMfa,
            s.AllowGuestInvites,
            s.DefaultMemberRole.ToString(),
            s.InvitationExpiryDays));
    }
}
