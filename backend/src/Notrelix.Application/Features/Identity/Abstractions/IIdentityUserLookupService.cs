using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Features.Identity.Abstractions;

public interface IIdentityUserLookupService
{
    Task<IdentityUserSnapshot?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed record IdentityUserSnapshot(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    UserStatus Status);
