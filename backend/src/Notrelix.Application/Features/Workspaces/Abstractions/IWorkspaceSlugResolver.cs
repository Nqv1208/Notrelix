namespace Notrelix.Application.Features.Workspaces.Abstractions;

/// <summary>The public card of a workspace resolved by account and slug.</summary>
public sealed record ResolvedWorkspaceCard(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool IsPersonal,
    DateTimeOffset CreatedAt,
    long Version,
    string? Size,
    string? Language,
    int MemberCount);

/// <summary>
/// Workspaces-owned read port for the anonymous resolve-by-slug endpoint. The adapter reads exactly one active
/// workspace through an owner-defined function; the caller has no session and no tenant, so no row policy is
/// widened for it.
/// </summary>
public interface IWorkspaceSlugResolver
{
    Task<ResolvedWorkspaceCard?> ResolveAsync(Guid accountId, string slug, CancellationToken cancellationToken);
}
