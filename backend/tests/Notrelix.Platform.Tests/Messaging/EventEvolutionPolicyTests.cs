using FluentAssertions;
using Notrelix.Platform.Messaging.Contracts.Evolution;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging;

public sealed class EventEvolutionPolicyTests
{
    [Fact]
    public void WorkRevisionEvents_Declare_DrainBeforeCutover_And_NoSyntheticUpcast()
    {
        EventEvolutionPolicyRegistry.GetAll().Should().BeEquivalentTo(
            new[] { "board.item.created", "board_item.moved", "board_item.archived" }
                .Select(name => new EventEvolutionPolicy(
                    name,
                    1,
                    2,
                    EventEvolutionDisposition.DrainBeforeCutover,
                    "V1 cannot mutate the V2 revision projection; rebuild from the authoritative Work producer snapshot.",
                    "Drain or quarantine every V1 queue before V2-only consumers become authoritative.",
                    false)),
            options => options.WithStrictOrdering());
    }

}
