using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Onboarding.Queries.CheckWorkspaceSlug;

/// <param name="Status"><c>free</c>, <c>taken</c> or <c>invalid</c>.</param>
/// <param name="Suggestion">The next free address; present only when <paramref name="Status"/> is <c>taken</c>.</param>
/// <param name="Reason">The stable rule code the address breaks; present only when <paramref name="Status"/> is <c>invalid</c>:
/// <c>SharedKernel_Slug_InvalidFormat</c>, <c>Workspaces_Workspace_SlugTooShort</c>, <c>Workspaces_Workspace_SlugTooLong</c> or <c>Workspaces_Workspace_SlugReserved</c>.</param>
public record WorkspaceSlugCheckDto(string Status, string? Suggestion, string? Reason = null);

public static class WorkspaceSlugStatuses
{
    public const string Free = "free";
    public const string Taken = "taken";
    public const string Invalid = "invalid";
}

/// <summary>
/// Whether a workspace address is usable in the caller's Account (accepted 2026-10-23: unique within the
/// Account only, never global). Same authorization as creating a workspace, because it answers only for
/// the Account the caller may create in. Active (not deleted) workspaces of the Account hold addresses,
/// exactly as the unique index does. The address is checked after trimming and lower-casing against the
/// same rule creation enforces (<see cref="WorkspaceSlugRules"/>: 3 to 63 characters, not reserved).
/// </summary>
public record CheckWorkspaceSlugQuery(string? Slug)
    : IQuery<Result<WorkspaceSlugCheckDto>>, IAuthenticatedRequest, IReadRequest, IAccountRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.CreateWorkspace;
    ResourceRef? IRequirePermission.Resource => null;
}

public class CheckWorkspaceSlugQueryHandler : IRequestHandler<CheckWorkspaceSlugQuery, Result<WorkspaceSlugCheckDto>>
{
    private const int SuggestionAttempts = 20;

    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public CheckWorkspaceSlugQueryHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result<WorkspaceSlugCheckDto>> Handle(CheckWorkspaceSlugQuery request, CancellationToken ct)
    {
        var violation = WorkspaceSlugRules.FindViolation(request.Slug);
        if (violation is not null)
            return Result<WorkspaceSlugCheckDto>.Success(new WorkspaceSlugCheckDto(WorkspaceSlugStatuses.Invalid, null, violation));

        var accountId = _requestContext.RequireAccountId();
        var slug = WorkspaceSlugRules.Normalize(request.Slug);

        var candidates = new List<string> { slug };
        for (var n = 2; n < SuggestionAttempts + 2; n++)
        {
            var suffix = "-" + n;
            candidates.Add((slug.Length + suffix.Length > WorkspaceSlugRules.MaxLength ? slug[..(WorkspaceSlugRules.MaxLength - suffix.Length)].TrimEnd('-') : slug) + suffix);
        }

        var taken = (await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.AccountId == accountId && w.DeletedAt == null && candidates.Contains(w.Slug))
            .Select(w => w.Slug)
            .ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);

        if (!taken.Contains(slug))
            return Result<WorkspaceSlugCheckDto>.Success(new WorkspaceSlugCheckDto(WorkspaceSlugStatuses.Free, null));

        var suggestion = candidates.Skip(1).FirstOrDefault(c => !taken.Contains(c))
            ?? (slug.Length > WorkspaceSlugRules.MaxLength - 7 ? slug[..(WorkspaceSlugRules.MaxLength - 7)].TrimEnd('-') : slug) + "-" + Guid.NewGuid().ToString("N")[..6];
        return Result<WorkspaceSlugCheckDto>.Success(new WorkspaceSlugCheckDto(WorkspaceSlugStatuses.Taken, suggestion));
    }
}
