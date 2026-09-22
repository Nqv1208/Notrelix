using Notrelix.Infrastructure.Messaging;

namespace Notrelix.Architecture.Tests.Events;

public sealed class EventEvolutionCompatibilityArchitectureTests
{
    [Fact]
    public void WorkRevisionV2Contracts_Are_Not_BackwardCompatible()
    {
        var definitions = ContractRegistrySetup.GetContractDefinitions()
            .Where(d => d.Name is "board.item.created" or "board_item.moved" or "board_item.archived")
            .ToList();

        definitions.Should().HaveCount(6);
        definitions.Should().OnlyContain(d => d.Compatibility == SchemaCompatibility.None);
    }

    [Fact]
    public void ContractRegistry_UsesTheCanonicalEvolutionPolicy()
    {
        EventEvolutionPolicyRegistry.GetAll()
            .Select(policy => policy.EventName)
            .Should()
            .BeEquivalentTo("board.item.created", "board_item.moved", "board_item.archived");

        EventEvolutionPolicyRegistry.GetCompatibility("unclassified.event", 1)
            .Should()
            .Be(SchemaCompatibility.Backward);
    }
}
