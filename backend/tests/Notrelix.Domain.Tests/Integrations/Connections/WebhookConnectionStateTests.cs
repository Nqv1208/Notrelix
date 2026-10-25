using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class WebhookConnectionStateTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    private static IntegrationConnection Webhook() => IntegrationConnection.Create(
        Guid.NewGuid(), Guid.NewGuid(), IntegrationProvider.Webhook, Actor, Now, targetUrl: " https://hooks.example.test/in ");

    [Fact]
    public void Create_KeepsTheTrimmedTargetAndStartsActive()
    {
        var connection = Webhook();

        connection.TargetUrl.Should().Be("https://hooks.example.test/in");
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.LastSuccessAt.Should().BeNull();
    }

    [Fact]
    public void PauseAndResume_ToggleTheStatus()
    {
        var connection = Webhook();

        connection.Pause(Actor, Now);
        connection.Status.Should().Be(IntegrationConnectionStatus.Paused);

        connection.Resume(Actor, Now.AddMinutes(1));
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
    }

    [Fact]
    public void Pause_ARevokedConnection_IsRefused()
    {
        var connection = Webhook();
        connection.Disconnect(Actor, Now);

        var act = () => connection.Pause(Actor, Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RecordFailure_ShowsTheErrorUntilASuccessClearsTheStatus()
    {
        var connection = Webhook();

        connection.RecordFailure("HTTP 500", Now);
        connection.Status.Should().Be(IntegrationConnectionStatus.Error);
        connection.ErrorDetail.Should().Be("HTTP 500");
        connection.LastErrorAt.Should().Be(Now);

        connection.RecordSuccess(Now.AddMinutes(1));
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);
        connection.LastSuccessAt.Should().Be(Now.AddMinutes(1));
        connection.ErrorDetail.Should().Be("HTTP 500", "the last error stays on record");
    }

    [Fact]
    public void RecordFailureAndSuccess_DoNotUnpauseAPausedConnection()
    {
        var connection = Webhook();
        connection.Pause(Actor, Now);

        connection.RecordFailure("HTTP 500", Now.AddMinutes(1));
        connection.RecordSuccess(Now.AddMinutes(2));

        connection.Status.Should().Be(IntegrationConnectionStatus.Paused);
        connection.LastSuccessAt.Should().Be(Now.AddMinutes(2));
    }
}
