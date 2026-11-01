using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;

public record UpdateWorkspaceSettingsCommand(
    Guid WorkspaceId,
    bool AllowPublicSharing,
    bool EnforceMfa,
    bool AllowGuestInvites,
    string DefaultMemberRole,
    int InvitationExpiryDays,
    long ExpectedVersion,
    int? GuestDefaultExpiryDays = null,
    string? Size = null,
    string? Language = null,
    string? TimeZone = null,
    string? WeekStart = null
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class UpdateWorkspaceSettingsCommandHandler : IRequestHandler<UpdateWorkspaceSettingsCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public UpdateWorkspaceSettingsCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IAuditService auditService)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
    }

    public async Task<Result> Handle(UpdateWorkspaceSettingsCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var defaultRole = Enum.Parse<WorkspaceRole>(request.DefaultMemberRole, ignoreCase: true);
        var settings = WorkspaceSettings.Create(
            request.AllowPublicSharing,
            request.EnforceMfa,
            request.AllowGuestInvites,
            defaultRole,
            request.InvitationExpiryDays,
            request.GuestDefaultExpiryDays,
            // Not sent means keep: a client that predates these fields must not clear them.
            request.Size ?? workspace.Settings.Size,
            request.Language ?? workspace.Settings.Language,
            request.TimeZone ?? workspace.Settings.TimeZone,
            request.WeekStart ?? workspace.Settings.WeekStart);

        var before = workspace.Settings;
        var versionBefore = workspace.Version;
        workspace.UpdateSettings(settings, _requestContext.UserId, _dateTimeProvider.UtcNow);

        // A settings change is recorded for Governance; an unchanged write is a no-op and records nothing.
        // Opening sharing or dropping MFA enforcement weakens the workspace's posture, so it is a warning.
        if (workspace.Version != versionBefore)
        {
            var weakened = (settings.AllowPublicSharing && !before.AllowPublicSharing)
                || (before.EnforceMfa && !settings.EnforceMfa);
            await _auditService.RecordAsync(
                workspace.Id,
                _requestContext.UserId,
                "UpdateWorkspaceSettings",
                ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), workspace.Id, workspace.Id),
                AuditMetadata.Create(),
                weakened ? AuditSeverity.Warning : AuditSeverity.Info,
                cancellationToken: ct);
        }

        return Result.Success();
    }
}
