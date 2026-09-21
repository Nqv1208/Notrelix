using System.Reflection;

namespace Notrelix.Infrastructure.Messaging;

public static class ContractRegistrySetup
{
    public static IReadOnlyList<ContractDefinition> GetContractDefinitions()
    {
        var assembly = typeof(IIntegrationEvent).Assembly;

        var eventTypes = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IIntegrationEvent).IsAssignableFrom(t))
            .ToList();

        var definitions = new List<ContractDefinition>(eventTypes.Count);

        foreach (var type in eventTypes)
        {
            var attr = type.GetCustomAttribute<EventNameAttribute>();
            if (attr is null)
                continue;

            var compatibility = attr.Name is "board.item.created" or "board_item.moved" or "board_item.archived"
                ? SchemaCompatibility.None
                : SchemaCompatibility.Backward;

            definitions.Add(new ContractDefinition
            {
                Name = attr.Name,
                Version = attr.Version,
                IntegrationEventType = type,
                Classification = EventClassification.Business,
                Compatibility = compatibility,
            });
        }

        return definitions;
    }
}
