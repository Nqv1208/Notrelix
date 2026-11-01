using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Lifecycle;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;

public record RestoreWorkspaceCommand(
    Guid WorkspaceId,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest, IWorkspaceReachesDeletedRequest
{
    public PermissionAction Action => PermissionAction.RestoreWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class RestoreWorkspaceCommandHandler : IRequestHandler<RestoreWorkspaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public RestoreWorkspaceCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IAuditService auditService)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
    }

    public async Task<Result> Handle(RestoreWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var versionBefore = workspace.Version;
        workspace.Restore(_requestContext.UserId, _dateTimeProvider.UtcNow);

        if (workspace.Version != versionBefore)
        {
            await _auditService.RecordAsync(
                workspace.Id,
                _requestContext.UserId,
                "RestoreWorkspace",
                ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), workspace.Id, workspace.Id),
                AuditMetadata.Create(),
                AuditSeverity.Warning,
                cancellationToken: ct);
        }

        return Result.Success();
    }
}
