using Notrelix.Application.Features.Integrations.Connections;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Application.Tests.Features.Integrations;

public class IntegrationSyncHealthTests
{
    [Fact]
    public void TheSetIsClosedAndExactlyTheProductValues()
    {
        IntegrationSyncHealth.All.Should().Equal(
            "idle", "syncing", "degraded", "rate-limited", "failed", "reauthorization-required", "resync-required");
    }

    [Theory]
    [InlineData(IntegrationConnectionStatus.Active, "idle")]
    [InlineData(IntegrationConnectionStatus.Paused, "idle")]
    [InlineData(IntegrationConnectionStatus.Error, "failed")]
    [InlineData(IntegrationConnectionStatus.Expired, "reauthorization-required")]
    public void Health_FollowsTheStoredState(IntegrationConnectionStatus status, string expected)
    {
        IntegrationSyncHealth.Of(status).Should().Be(expected);
    }

    [Fact]
    public void EveryStatus_MapsToAValueOfTheClosedSet()
    {
        foreach (var status in Enum.GetValues<IntegrationConnectionStatus>())
        {
            IntegrationSyncHealth.All.Should().Contain(IntegrationSyncHealth.Of(status), $"status {status}");
        }
    }

    [Fact]
    public void Dto_CarriesHealthAsASeparateFieldFromTheStatus()
    {
        var connection = IntegrationConnection.Create(
            Guid.NewGuid(), Guid.NewGuid(), IntegrationProvider.Webhook, Guid.NewGuid(), DateTimeOffset.UtcNow, targetUrl: "https://hooks.example.test/a");
        connection.RecordFailure("HTTP 500", DateTimeOffset.UtcNow);

        var dto = IntegrationConnectionMapper.ToDto(connection);

        dto.Status.Should().Be("error", "the status is not changed by the health field");
        dto.SyncHealth.Should().Be("failed");
    }

    [Fact]
    public void Dto_OfAHealthyConnection_IsIdle()
    {
        var connection = IntegrationConnection.Create(
            Guid.NewGuid(), Guid.NewGuid(), IntegrationProvider.Webhook, Guid.NewGuid(), DateTimeOffset.UtcNow, targetUrl: "https://hooks.example.test/a");

        IntegrationConnectionMapper.ToDto(connection).SyncHealth.Should().Be("idle");
    }
}
