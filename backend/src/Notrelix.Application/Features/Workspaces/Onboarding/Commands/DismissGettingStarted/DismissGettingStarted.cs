using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Onboarding;

namespace Notrelix.Application.Features.Workspaces.Onboarding.Commands.DismissGettingStarted;

/// <summary>
/// The caller hides the getting-started card in this workspace. Stored per user and workspace; dismissing
/// again is a semantic no-op. Any member may do it (<c>ViewWorkspace</c>); the subject is always the principal.
/// </summary>
public record DismissGettingStartedCommand(Guid WorkspaceId)
    : ICommand<Result>, IAuthenticatedRequest, IWriteRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class DismissGettingStartedCommandHandler : IRequestHandler<DismissGettingStartedCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public DismissGettingStartedCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result> Handle(DismissGettingStartedCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var exists = await _context.GettingStartedDismissals
            .AnyAsync(d => d.WorkspaceId == request.WorkspaceId && d.UserId == userId, ct);
        if (exists)
            return Result.Success();

        _context.GettingStartedDismissals.Add(GettingStartedDismissal.Create(
            _requestContext.RequireAccountId(), request.WorkspaceId, userId, _clock.UtcNow));
        return Result.Success();
    }
}
