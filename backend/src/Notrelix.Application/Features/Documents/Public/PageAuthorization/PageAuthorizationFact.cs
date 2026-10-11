namespace Notrelix.Application.Features.Documents.Public.PageAuthorization;

/// <summary>
/// Producer-owned page authorization snapshot for one Documents page.
/// Documents owns page lifecycle and visibility; this fact exposes exactly
/// the authorization-relevant projection a consumer needs — never Domain
/// aggregates or persistence types. <c>SpaceId</c> is the Workspaces Space the
/// page is placed in (null = Workspace root), which Governance uses for
/// space-scoped access (GOV-025).
/// </summary>
public sealed record PageAuthorizationFact(
    Guid PageId,
    Guid WorkspaceId,
    bool Exists,
    bool IsActive,
    string? Visibility,
    Guid? SpaceId = null);