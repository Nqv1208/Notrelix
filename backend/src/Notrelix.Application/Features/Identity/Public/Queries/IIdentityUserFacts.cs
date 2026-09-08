using Notrelix.Application.Features.Identity.Public.Facts;

namespace Notrelix.Application.Features.Identity.Public.Queries;

/// <summary>
/// Producer-owned public query surface for stable Identity user facts.
/// Returns null when the user does not exist.
/// </summary>
public interface IIdentityUserFacts
{
    Task<IdentityUserFact?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
