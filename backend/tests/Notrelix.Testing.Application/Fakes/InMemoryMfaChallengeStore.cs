using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// In-memory single-use challenge store for tests that need real
/// issue/consume semantics without Redis. Consumes exactly once.
/// </summary>
public sealed class InMemoryMfaChallengeStore : IMfaChallengeStore
{
    private readonly Dictionary<string, MfaChallengePayload> _items = new();

    private readonly Dictionary<string, int> _attempts = new();

    private readonly IDateTimeProvider _clock;

    public InMemoryMfaChallengeStore(IDateTimeProvider clock)
    {
        _clock = clock;
    }

    public Task StoreAsync(string token, MfaChallengePayload payload, TimeSpan ttl, CancellationToken ct = default)
    {
        _items[token] = payload;
        return Task.CompletedTask;
    }

    public Task<MfaChallengePayload?> PeekAsync(string token, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        if (_items.TryGetValue(token, out var payload) && payload.ExpiresAt >= now)
        {
            return Task.FromResult<MfaChallengePayload?>(payload);
        }

        return Task.FromResult<MfaChallengePayload?>(null);
    }

    public Task<MfaChallengePayload?> ConsumeAsync(string token, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        if (_items.TryGetValue(token, out var payload) && payload.ExpiresAt >= now)
        {
            _items.Remove(token);
            return Task.FromResult<MfaChallengePayload?>(payload);
        }

        return Task.FromResult<MfaChallengePayload?>(null);
    }

    public Task<MfaChallengeAttempt> RecordAttemptAsync(
        string token, int maxAttempts, TimeSpan ttl, CancellationToken ct = default)
    {
        var attempts = _attempts.TryGetValue(token, out var prior) ? prior + 1 : 1;
        _attempts[token] = attempts;

        var exceeded = attempts > maxAttempts;
        if (exceeded)
        {
            _items.Remove(token);
        }

        return Task.FromResult(new MfaChallengeAttempt(attempts, exceeded));
    }
}
