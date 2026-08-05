using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;

public record UpdateWorkspaceProfileCommand(
    Guid WorkspaceId,
    string? Name,
    string? Description,
    long ExpectedVersion
) : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class UpdateWorkspaceProfileCommandHandler : IRequestHandler<UpdateWorkspaceProfileCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateWorkspaceProfileCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateWorkspaceProfileCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var now = _dateTimeProvider.UtcNow;
        var userId = _requestContext.UserId;

        if (request.Name is not null)
            workspace.Rename(request.Name, userId, now);

        if (request.Description is not null)
            workspace.UpdateDescription(request.Description, userId, now);

        return Result.Success();
    }
}
