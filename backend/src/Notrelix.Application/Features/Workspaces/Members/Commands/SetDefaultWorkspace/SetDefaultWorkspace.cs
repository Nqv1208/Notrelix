using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Public.DefaultWorkspace;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.SetDefaultWorkspace;

/// <summary>
/// Chooses the Workspace that opens with the app for the caller. Any active member may choose any
/// Workspace they belong to (<c>ViewWorkspace</c> on it); the pipeline answers not-found or forbidden
/// for a Workspace the caller cannot open, and the choice itself is Identity's per-person fact. The
/// subject is always the principal. An archived Workspace is read-only and not where the app should open,
/// so choosing it is refused. Choosing the current default succeeds and changes nothing.
/// </summary>
public record SetDefaultWorkspaceCommand(Guid WorkspaceId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SetDefaultWorkspaceCommandValidator : AbstractValidator<SetDefaultWorkspaceCommand>
{
    public SetDefaultWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}

public class SetDefaultWorkspaceCommandHandler : IRequestHandler<SetDefaultWorkspaceCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IIdentityDefaultWorkspace _defaultWorkspace;

    public SetDefaultWorkspaceCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IIdentityDefaultWorkspace defaultWorkspace)
    {
        _context = context;
        _requestContext = requestContext;
        _defaultWorkspace = defaultWorkspace;
    }

    public async Task<Result> Handle(SetDefaultWorkspaceCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;

        // Only an active member of an active Workspace can make it the one that opens.
        var isActiveMember = await _context.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(m => m.WorkspaceId == request.WorkspaceId
                && m.UserId == userId
                && m.Status == WorkspaceMemberStatus.Active, ct);
        if (!isActiveMember)
        {
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);
        }

        var archived = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Archived, ct);
        if (archived)
        {
            throw new BusinessRuleException("An archived workspace cannot be the default workspace.");
        }

        await _defaultWorkspace.SetAsync(userId, request.WorkspaceId, ct);
        return Result.Success();
    }
}
