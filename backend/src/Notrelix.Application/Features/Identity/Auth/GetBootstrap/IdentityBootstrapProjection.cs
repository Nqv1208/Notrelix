namespace Notrelix.Application.Features.Identity.Auth.GetBootstrap;

/// <summary>
/// Immutable bootstrap projection returned by <see cref="IIdentityBootstrapReadPort"/>.
/// </summary>
public sealed record IdentityBootstrapProjection(
    BootstrapUserProjection User,
    IReadOnlyList<BootstrapWorkspaceProjection> Workspaces,
    Guid? PersonalWorkspaceId);

public sealed record BootstrapUserProjection(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    bool EmailConfirmed);

public sealed record BootstrapWorkspaceProjection(
    Guid Id,
    string Name,
    string Slug,
    string Role);
