using Notrelix.Application.Features.Identity.Sso;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>A single-use state store: consuming a state removes it, so a replay finds nothing.</summary>
public sealed class InMemorySsoStateStore : ISsoStateStore
{
    private readonly Dictionary<string, SsoLoginState> _states = new(StringComparer.Ordinal);

    public IReadOnlyCollection<SsoLoginState> Stored => _states.Values;

    public Task StoreAsync(SsoLoginState state, TimeSpan ttl, CancellationToken cancellationToken)
    {
        _states[state.State] = state;
        return Task.CompletedTask;
    }

    public Task<SsoLoginState?> ConsumeAsync(string state, CancellationToken cancellationToken)
    {
        var found = _states.Remove(state, out var value) ? value : null;
        return Task.FromResult(found);
    }
}
