using Notrelix.Application.Common.Caching;
using Notrelix.Infrastructure.Identity.Mfa;

namespace Notrelix.Infrastructure.Tests.Identity.Mfa;

public class MfaCodeUseGuardTests
{
    private readonly Dictionary<string, long> _counters = new();
    private readonly MfaCodeUseGuard _guard;

    public MfaCodeUseGuardTests()
    {
        // The guard's whole contract is Redis' atomic increment: the first caller sees 1.
        var cache = new Mock<IRedisCacheService>();
        cache.Setup(c => c.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, TimeSpan? _, CancellationToken _) =>
                _counters[key] = _counters.GetValueOrDefault(key) + 1);
        _guard = new MfaCodeUseGuard(cache.Object);
    }

    [Fact]
    public async Task ATotpStep_IsClaimedOnce_AndARepeatIsRefused()
    {
        var user = Guid.NewGuid();

        (await _guard.TryClaimTotpStepAsync(user, 100, CancellationToken.None)).Should().BeTrue();
        (await _guard.TryClaimTotpStepAsync(user, 100, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task ATotpClaim_BelongsToOnePersonAndOneStep()
    {
        var user = Guid.NewGuid();
        await _guard.TryClaimTotpStepAsync(user, 100, CancellationToken.None);

        (await _guard.TryClaimTotpStepAsync(user, 101, CancellationToken.None)).Should().BeTrue();
        (await _guard.TryClaimTotpStepAsync(Guid.NewGuid(), 100, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task ARecoveryCode_IsClaimedOnce_AndARepeatIsRefused()
    {
        var user = Guid.NewGuid();

        (await _guard.TryClaimRecoveryCodeAsync(user, "hash-1", CancellationToken.None)).Should().BeTrue();
        (await _guard.TryClaimRecoveryCodeAsync(user, "hash-1", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task ARecoveryClaim_BelongsToOnePersonAndOneCode()
    {
        var user = Guid.NewGuid();
        await _guard.TryClaimRecoveryCodeAsync(user, "hash-1", CancellationToken.None);

        (await _guard.TryClaimRecoveryCodeAsync(user, "hash-2", CancellationToken.None)).Should().BeTrue();
        (await _guard.TryClaimRecoveryCodeAsync(Guid.NewGuid(), "hash-1", CancellationToken.None)).Should().BeTrue();
    }
}
