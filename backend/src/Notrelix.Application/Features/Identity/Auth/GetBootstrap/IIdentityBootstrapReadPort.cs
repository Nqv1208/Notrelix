namespace Notrelix.Application.Features.Identity.Auth.GetBootstrap;

/// <summary>
/// Cross-context read port (spec 5.1): the bootstrap query reads Identity,
/// Workspaces and Accounts through this projection port instead of injecting
/// three DbContext ports. Null is returned when the authenticated user
/// does not exist.
/// </summary>
public interface IIdentityBootstrapReadPort
{
    Task<IdentityBootstrapProjection?> GetAsync(
        Guid authenticatedUserId,
        CancellationToken cancellationToken);
}
