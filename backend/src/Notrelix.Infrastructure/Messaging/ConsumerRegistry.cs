using System.Collections.Concurrent;

namespace Notrelix.Infrastructure.Messaging;

/// <summary>
/// Canonical source-level maturity classification for registered consumers
/// (IAREQ133 / P13-EVT-001B). Registry presence alone does not prove an
/// implemented downstream capability — the maturity value is explicit,
/// reviewed metadata, never inferred from naming or runtime traffic.
/// </summary>
public enum ConsumerMaturity
{
    /// <summary>The consumer performs its owned business effect.</summary>
    Implemented = 1,

    /// <summary>The consumer exists as a placeholder (e.g. logging only) and does not yet perform its business effect.</summary>
    Stub = 2,
}

public sealed record ConsumerDefinition
{
    public required string ConsumerName { get; init; }
    public required string EventName { get; init; }
    public int EventVersion { get; init; }
    public string? EndpointName { get; init; }
    public int ConcurrencyLimit { get; init; } = 1;
    public bool OrderingRequired { get; init; }
    public int PoisonThreshold { get; init; } = 5;
    public string BoundedContext { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Idempotent { get; init; } = true;

    /// <summary>Explicit maturity classification. There is no implicit default.</summary>
    public required ConsumerMaturity Maturity { get; init; }
}

public interface IConsumerRegistry
{
    IReadOnlyList<ConsumerDefinition> GetConsumers(string eventName);
    IReadOnlyList<ConsumerDefinition> GetAll();
    bool HasConsumer(string eventName);
}

public sealed class ConsumerRegistry : IConsumerRegistry
{
    private readonly ConcurrentDictionary<string, List<ConsumerDefinition>> _consumers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ConsumerDefinition> _all;

    public ConsumerRegistry(IEnumerable<ConsumerDefinition> consumers)
    {
        _all = consumers.ToList();
        foreach (var consumer in _all)
        {
            _consumers.AddOrUpdate(
                consumer.EventName,
                _ => [consumer],
                (_, list) => { list.Add(consumer); return list; });
        }
    }

    public IReadOnlyList<ConsumerDefinition> GetConsumers(string eventName)
    {
        return _consumers.TryGetValue(eventName, out var list)
            ? list.AsReadOnly()
            : [];
    }

    public IReadOnlyList<ConsumerDefinition> GetAll() => _all.AsReadOnly();

    public bool HasConsumer(string eventName) => _consumers.ContainsKey(eventName);
}
