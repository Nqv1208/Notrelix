using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;

public record RestoreWorkspaceCommand(Guid WorkspaceId)
    : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class RestoreWorkspaceCommandHandler : IRequestHandler<RestoreWorkspaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestoreWorkspaceCommandHandler(IWorkspaceDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestoreWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        workspace.Restore(_currentUser.UserId, _dateTimeProvider.UtcNow);

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
