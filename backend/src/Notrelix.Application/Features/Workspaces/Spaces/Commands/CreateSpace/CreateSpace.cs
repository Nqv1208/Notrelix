using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;

public record CreateSpaceCommand(
    Guid WorkspaceId,
    string Name,
    string Visibility,
    string? Description
) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail
{
    public PermissionAction Action => PermissionAction.CreateWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CreateSpaceCommandHandler : IRequestHandler<CreateSpaceCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSpaceCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var visibility = Enum.Parse<SpaceVisibility>(request.Visibility, ignoreCase: true);
        var space = Space.Create(
            workspace.AccountId,
            request.WorkspaceId,
            request.Name,
            visibility,
            _requestContext.UserId,
            _dateTimeProvider.UtcNow,
            description: request.Description);

        _context.Spaces.Add(space);
        return Result<Guid>.Success(space.Id);
    }
}
