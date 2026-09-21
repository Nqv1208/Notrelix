using Notrelix.Application.Common.Events;
using Notrelix.Infrastructure.Messaging;
using FluentAssertions;
using Xunit;

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
}
