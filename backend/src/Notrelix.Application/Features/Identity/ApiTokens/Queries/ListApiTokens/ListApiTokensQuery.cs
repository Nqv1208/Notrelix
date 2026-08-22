using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.ApiTokens.DTOs;

namespace Notrelix.Application.Features.Identity.ApiTokens.Queries.ListApiTokens;

/// <summary>
/// Lists API token metadata for a workspace. The raw secret is never returned:
/// it exists exactly once at issuance and cannot be recovered from the digest.
/// </summary>
public sealed record ListApiTokensQuery(Guid WorkspaceId)
    : IQuery<Result<IReadOnlyList<ApiTokenSummaryDto>>>, IWorkspaceRequest, IAuthenticatedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class ListApiTokensQueryHandler
    : IRequestHandler<ListApiTokensQuery, Result<IReadOnlyList<ApiTokenSummaryDto>>>
{
    private readonly IIdentityDbContext _context;

    public ListApiTokensQueryHandler(IIdentityDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<ApiTokenSummaryDto>>> Handle(
        ListApiTokensQuery request, CancellationToken ct)
    {
        var tokens = await _context.ApiTokens
            .AsNoTracking()
            .Where(t => t.WorkspaceId == request.WorkspaceId)
            .ToListAsync(ct);

        var result = tokens
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new ApiTokenSummaryDto(
            t.Id,
            t.Name,
            t.Scopes?.ToJson(),
            t.Status.ToString(),
            t.LastUsedAt,
            t.ExpiresAt,
            t.CreatedAt,
            t.RevokedAt)).ToList();

        return Result<IReadOnlyList<ApiTokenSummaryDto>>.Success(result);
    }
}