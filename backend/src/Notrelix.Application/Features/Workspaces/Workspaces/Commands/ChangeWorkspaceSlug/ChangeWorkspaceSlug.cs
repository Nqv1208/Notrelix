using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.ChangeWorkspaceSlug;

/// <summary>
/// Changes the workspace address. Owner only, through the policy engine (<c>ManageWorkspace</c>); a guarded
/// write, so the expected version is its idempotency and a stale one answers 412. The same address is a no-op.
/// Addresses are unique within the Account (the rule <c>CheckWorkspaceSlug</c> reports); a taken one is a
/// conflict. Links use ids, so nothing redirects from the old address.
/// </summary>
public record ChangeWorkspaceSlugCommand(
    Guid WorkspaceId,
    string Slug,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class ChangeWorkspaceSlugCommandHandler : IRequestHandler<ChangeWorkspaceSlugCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeWorkspaceSlugCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ChangeWorkspaceSlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var slug = WorkspaceSlugRules.Normalize(request.Slug);
        if (slug == workspace.Slug)
            return Result.Success();

        WorkspaceSlugRules.Validate(slug);

        var taken = await _context.Workspaces.AnyAsync(
            w => w.AccountId == workspace.AccountId && w.Id != workspace.Id && w.Slug == slug, ct);
        if (taken)
            throw new ConflictException("The workspace address is already taken in this account.", CreateWorkspaceCommandHandler.SlugTakenErrorCode);

        workspace.ChangeSlug(slug, _requestContext.UserId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
