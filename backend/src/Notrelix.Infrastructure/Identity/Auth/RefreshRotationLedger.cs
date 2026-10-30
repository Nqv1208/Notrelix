using Notrelix.Domain.Identity.Sessions;
using Notrelix.Application.Features.Identity.Auth.Services;

namespace Notrelix.Infrastructure.Identity.Auth;

/// <summary>
/// Redis-backed rotation coordination. The claim is the first atomic increment of a key; the held
/// credential expires shortly after the grace window, and the database (rotation time) decides whether
/// the window is still open, so this store only has to carry the value.
/// </summary>
public sealed class RefreshRotationLedger : IRefreshRotationLedger
{
    private static readonly TimeSpan HoldTtl = SessionLifetimePolicy.RotationGrace + TimeSpan.FromSeconds(5);

    private readonly IRedisCacheService _cache;

    public RefreshRotationLedger(IRedisCacheService cache)
    {
        _cache = cache;
    }

    public async Task<bool> TryClaimAsync(string presentedHash, CancellationToken ct) =>
        await _cache.IncrementAsync($"identity:refresh:claim:{presentedHash}", HoldTtl, ct) == 1;

    public Task RecordSuccessorAsync(string presentedHash, string newRefreshToken, CancellationToken ct) =>
        _cache.SetAsync($"identity:refresh:successor:{presentedHash}", newRefreshToken, HoldTtl, ct);

    public Task<string?> RecallSuccessorAsync(string presentedHash, CancellationToken ct) =>
        _cache.GetAsync<string>($"identity:refresh:successor:{presentedHash}", ct);
}
