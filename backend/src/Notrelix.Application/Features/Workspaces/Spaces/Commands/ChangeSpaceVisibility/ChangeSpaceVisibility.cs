using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceVisibility;

public record ChangeSpaceVisibilityCommand(
    Guid WorkspaceId,
    Guid SpaceId,
    string Visibility
) : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ChangeSpaceVisibilityCommandHandler : IRequestHandler<ChangeSpaceVisibilityCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeSpaceVisibilityCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ChangeSpaceVisibilityCommand request, CancellationToken ct)
    {
        var space = await _context.Spaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var visibility = Enum.Parse<SpaceVisibility>(request.Visibility, ignoreCase: true);
        space.ChangeVisibility(visibility, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
