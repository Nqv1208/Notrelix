using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;

public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    bool IsPersonal
) : ICommand<Result<Guid>>, IAccountRequest, IRequirePermission, ITransactionalRequest
{
    public PermissionAction Action => PermissionAction.CreateWorkspace;
    public ResourceRef? Resource => null;
}

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateWorkspaceCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken ct)
    {
        var accountId = _requestContext.RequireAccountId();
        var slug = Slug.GenerateFromName(request.Name);

        // Pre-check for UX — DB unique constraint is source of truth
        var slugExists = await _context.Workspaces
            .AnyAsync(w => w.AccountId == accountId && w.Slug == slug.Value, ct);

        var finalSlug = slugExists
            ? slug.Value + "-" + Guid.NewGuid().ToString("N")[..6]
            : slug.Value;

        var creationResult = WorkspaceFactory.CreateWithOwner(
            accountId,
            _requestContext.UserId, request.Name, finalSlug,
            _dateTimeProvider.UtcNow, request.IsPersonal,
            request.Description);

        _context.Workspaces.Add(creationResult.Workspace);
        _context.WorkspaceMembers.Add(creationResult.OwnerMember);

        return Result<Guid>.Success(creationResult.Workspace.Id);
    }
}
