namespace Notrelix.Application.Features.Documents.Public.PageAuthorization;

/// <summary>
/// Producer-owned page authorization snapshot for one Documents page.
/// Documents owns page lifecycle and visibility; this fact exposes exactly
/// the authorization-relevant projection a consumer needs — never Domain
/// aggregates or persistence types.
/// </summary>
public sealed record PageAuthorizationFact(
    Guid PageId,
    Guid WorkspaceId,
    bool Exists,
    bool IsActive,
    string? Visibility);