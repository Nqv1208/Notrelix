using Notrelix.Application.Features.Identity.OAuth.DTOs;

namespace Notrelix.Application.Features.Identity.OAuth.Abstractions;

public interface IOAuthStateStore
{
    Task StoreAsync(OAuthLoginState state, TimeSpan ttl, CancellationToken ct);
    Task<OAuthLoginState?> ConsumeAsync(string state, CancellationToken ct);
}
