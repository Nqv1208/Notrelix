using FluentAssertions;
using Notrelix.Application.Common.Events;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging;

public sealed class EventEvolutionPolicyTests
{
    [Fact]
    public void WorkRevisionEvents_Declare_ExplicitCutoverRecoveryAndUpcastModes()
    {
        EventEvolutionPolicyRegistry.GetAll().Should().BeEquivalentTo(
            new[] { "board.item.created", "board_item.moved", "board_item.archived" }
                .Select(name => new EventEvolutionPolicy(
                    name,
                    1,
                    2,
                    UpcastMode.Forbidden,
                    CutoverMode.DrainBeforeCutover,
                    RecoveryMode.RebuildFromAuthority)),
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void WorkRevisionEvents_Derive_IncompatibleSchema_WithoutSyntheticUpcast()
    {
        foreach (var policy in EventEvolutionPolicyRegistry.GetAll())
        {
            policy.Compatibility.Should().Be(SchemaCompatibility.None);
            policy.SyntheticUpcastAllowed.Should().BeFalse();
        }
    }

}
