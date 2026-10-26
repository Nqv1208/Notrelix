using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;

public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    bool IsPersonal,
    string? Slug = null,
    string? Size = null,
    string? Language = null
) : ICommand<Result<Guid>>, IAuthenticatedRequest, IAccountRequest, IRequirePermission, IRequireVerifiedEmail, IWriteRequest
{
    public PermissionAction Action => PermissionAction.CreateWorkspace;
    public ResourceRef? Resource => null;
}

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<Guid>>
{
    public const string SlugTakenErrorCode = "workspaces.workspace.slug-taken";

    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceGrantProjectionService _grantProjection;

    public CreateWorkspaceCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IWorkspaceGrantProjectionService grantProjection)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
    }

    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken ct)
    {
        var accountId = _requestContext.RequireAccountId();
        var finalSlug = string.IsNullOrWhiteSpace(request.Slug)
            ? await DeriveSlugAsync(accountId, request.Name, ct)
            : await EnsureChosenSlugFreeAsync(accountId, request.Slug, ct);

        var creationResult = WorkspaceFactory.CreateWithOwner(
            accountId,
            _requestContext.UserId, request.Name, finalSlug,
            _dateTimeProvider.UtcNow, request.IsPersonal,
            request.Description,
            request.Size,
            request.Language);

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

    // The slug is derived from the name when the caller chose none. The derived address must satisfy the
    // same rule as a chosen one (3 to 63 characters, not reserved), so a name that yields a short or
    // reserved word, or an address another workspace of the Account holds, gets a short random suffix.
    // The unique index stays the source of truth for races.
    private async Task<string> DeriveSlugAsync(Guid accountId, string name, CancellationToken ct)
    {
        const int suffixLength = 7;
        var slug = Slug.GenerateFromName(name).Value;
        if (slug.Length > WorkspaceSlugRules.MaxLength)
            slug = slug[..WorkspaceSlugRules.MaxLength].TrimEnd('-');

        var needsSuffix = slug.Length < WorkspaceSlugRules.MinLength
            || WorkspaceSlugRules.Reserved.Contains(slug)
            || await _context.Workspaces.AnyAsync(w => w.AccountId == accountId && w.Slug == slug, ct);
        if (!needsSuffix)
            return slug;

        var stem = slug.Length > WorkspaceSlugRules.MaxLength - suffixLength
            ? slug[..(WorkspaceSlugRules.MaxLength - suffixLength)].TrimEnd('-')
            : slug;
        return stem + "-" + Guid.NewGuid().ToString("N")[..6];
    }

    // A chosen address is never silently changed: it breaks the rule (rejected by the Domain with its
    // stable rule code) or is taken in the Account (conflict).
    private async Task<string> EnsureChosenSlugFreeAsync(Guid accountId, string chosen, CancellationToken ct)
    {
        WorkspaceSlugRules.Validate(chosen);
        var slug = WorkspaceSlugRules.Normalize(chosen);
        var taken = await _context.Workspaces.AnyAsync(w => w.AccountId == accountId && w.Slug == slug, ct);
        if (taken)
            throw new ConflictException("The workspace address is already taken in this account.", SlugTakenErrorCode);
        return slug;
    }
}
