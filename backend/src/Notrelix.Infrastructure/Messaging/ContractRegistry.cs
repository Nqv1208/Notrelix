using System.Collections.Concurrent;

namespace Notrelix.Infrastructure.Messaging;

public sealed class ContractRegistry : IContractRegistry
{
    private readonly ConcurrentDictionary<string, ContractDefinition> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Type, ContractDefinition> _byType = new();
    private readonly ConcurrentDictionary<(string Name, int Version), ContractDefinition> _byNameAndVersion = new();

    public ContractRegistry(IEnumerable<ContractDefinition> contracts)
    {
        foreach (var contract in contracts)
        {
            _byName[contract.Name] = contract;
            _byType[contract.IntegrationEventType] = contract;
            _byNameAndVersion[(contract.Name, contract.Version)] = contract;
        }
    }

    public ContractDefinition Get(string eventName, int version)
    {
        if (_byNameAndVersion.TryGetValue((eventName, version), out var contract))
            return contract;

        throw new InvalidOperationException(
            $"No contract registered for event '{eventName}' version {version}.");
    }

    public ContractDefinition GetByType(Type integrationEventType)
    {
        if (_byType.TryGetValue(integrationEventType, out var contract))
            return contract;

        throw new InvalidOperationException(
            $"No contract registered for integration event type '{integrationEventType.Name}'.");
    }

    public IReadOnlyList<ContractDefinition> GetAll() => _byType.Values.ToList();

    public bool IsRegistered(Type integrationEventType) => _byType.ContainsKey(integrationEventType);
}
