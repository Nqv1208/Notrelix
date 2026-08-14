using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;

public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    bool IsPersonal
) : ICommand<Result<Guid>>, IAccountRequest, IRequirePermission, IRequireVerifiedEmail, ITransactionalRequest
{
    public PermissionAction Action => PermissionAction.CreateWorkspace;
    public ResourceRef? Resource => null;
}

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGrantProjectionService _grantProjection;

    public CreateWorkspaceCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IAccessGrantProjectionService grantProjection)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
    }

    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken ct)
    {
        var accountId = _requestContext.RequireAccountId();
        var slug = Slug.GenerateFromName(request.Name);
        const int maxSlugLength = 128;
        const int suffixLength = 7;

        // Pre-check for UX — DB unique constraint is source of truth
        var slugExists = await _context.Workspaces
            .AnyAsync(w => w.AccountId == accountId && w.Slug == slug.Value, ct);

        var finalSlug = slugExists
            ? slug.Value.Length > maxSlugLength - suffixLength
                ? slug.Value[..(maxSlugLength - suffixLength)] + "-" + Guid.NewGuid().ToString("N")[..6]
                : slug.Value + "-" + Guid.NewGuid().ToString("N")[..6]
            : slug.Value.Length > maxSlugLength
                ? slug.Value[..maxSlugLength]
                : slug.Value;

        var creationResult = WorkspaceFactory.CreateWithOwner(
            accountId,
            _requestContext.UserId, request.Name, finalSlug,
            _dateTimeProvider.UtcNow, request.IsPersonal,
            request.Description);

        _context.Workspaces.Add(creationResult.Workspace);
        _context.WorkspaceMembers.Add(creationResult.OwnerMember);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            accountId,
            creationResult.Workspace.Id,
            _requestContext.UserId,
            WorkspaceRole.Owner,
            _dateTimeProvider.UtcNow,
            ct);

        return Result<Guid>.Success(creationResult.Workspace.Id);
    }
}
