using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// In-memory single-use store for VERIFIED step-up proofs, mirroring the
/// production Redis store semantics without Redis. Consumes exactly once.
/// </summary>
public sealed class InMemoryStepUpProofStore : IStepUpProofStore
{
    private readonly Dictionary<string, StepUpProofPayload> _items = new();

    private readonly IDateTimeProvider _clock;

    public InMemoryStepUpProofStore(IDateTimeProvider clock)
    {
        _clock = clock;
    }

    public Task StoreAsync(string token, StepUpProofPayload payload, TimeSpan ttl, CancellationToken ct = default)
    {
        _items[token] = payload;
        return Task.CompletedTask;
    }

    public Task<StepUpProofPayload?> ConsumeAsync(string token, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        if (_items.TryGetValue(token, out var payload) && payload.ExpiresAt >= now)
        {
            _items.Remove(token);
            return Task.FromResult<StepUpProofPayload?>(payload);
        }

        return Task.FromResult<StepUpProofPayload?>(null);
    }
}
