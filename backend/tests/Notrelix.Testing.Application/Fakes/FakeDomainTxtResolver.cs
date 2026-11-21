using Notrelix.Application.Features.Accounts.Abstractions;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>A TXT resolver that answers from a table, or simulates a DNS outage (no records), for domain verification tests.</summary>
public sealed class FakeDomainTxtResolver : IDomainTxtResolver
{
    private readonly Dictionary<string, List<string>> _records = new(StringComparer.OrdinalIgnoreCase);

    public bool Unavailable { get; set; }

    public int Lookups { get; private set; }

    public void Publish(string domain, string value)
    {
        if (!_records.TryGetValue(domain, out var list)) _records[domain] = list = [];
        list.Add(value);
    }

    public Task<IReadOnlyList<string>> ResolveTxtAsync(string domain, CancellationToken cancellationToken)
    {
        Lookups++;
        if (Unavailable || !_records.TryGetValue(domain, out var list)) return Task.FromResult<IReadOnlyList<string>>([]);
        return Task.FromResult<IReadOnlyList<string>>(list.ToList());
    }
}
