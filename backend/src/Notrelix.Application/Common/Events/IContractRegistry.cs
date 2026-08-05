namespace Notrelix.Application.Common.Events;

public enum SchemaCompatibility
{
    Backward,
    Forward,
    Full,
    None,
}

public sealed record ContractDefinition
{
    public string Name { get; init; } = string.Empty;
    public int Version { get; init; }
    public Type IntegrationEventType { get; init; } = null!;
    public EventClassification Classification { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool Deprecated { get; init; }
    public DateOnly? DeprecationDate { get; init; }
    public SchemaCompatibility Compatibility { get; init; } = SchemaCompatibility.Backward;
}

public interface IContractRegistry
{
    ContractDefinition Get(string eventName, int version);
    ContractDefinition GetByType(Type integrationEventType);
    IReadOnlyList<ContractDefinition> GetAll();
    bool IsRegistered(Type integrationEventType);
}
