using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;
using Xunit;

namespace Notrelix.Domain.Tests.Integrations;

public class IntegrationConnectionTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var connection = IntegrationConnection.Create(workspaceId, IntegrationProvider.Slack, Guid.NewGuid());

        connection.Provider.Should().Be(IntegrationProvider.Slack);
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationConnectionCreatedEvent);
    }
}
