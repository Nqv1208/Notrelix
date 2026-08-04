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
    long ExpectedVersion
) : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
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

    public UpdateWorkspaceSettingsCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
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
            request.InvitationExpiryDays);

        workspace.UpdateSettings(settings, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
