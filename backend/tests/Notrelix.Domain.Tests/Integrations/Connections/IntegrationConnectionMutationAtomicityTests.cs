using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class IntegrationConnectionMutationAtomicityTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static IntegrationConnection CreateActive() =>
        IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

    [Fact]
    public void MarkError_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.MarkError("err", Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [Fact]
    public void MarkError_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.MarkError("err", Actor, Now);
        var before = c.Version;
        c.MarkError("err", Actor, Now);
        c.Version.Should().Be(before);
    }

    [Fact]
    public void Reconnect_ShouldIncrementVersion()
    {
        var c = CreateActive();
        c.MarkError("err", Actor, Now);
        var before = c.Version;
        c.Reconnect(null, null, Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [Fact]
    public void Reconnect_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Reconnect(null, null, Actor, Now);
        c.Version.Should().Be(before);
    }

    [Fact]
    public void Disconnect_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Disconnect(Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [Fact]
    public void Disconnect_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.Disconnect(Actor, Now);
        var before = c.Version;
        c.Disconnect(Actor, Now);
        c.Version.Should().Be(before);
    }

    [Fact]
    public void AddScope_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.AddScope("read", Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [Fact]
    public void AddScope_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.AddScope("read", Actor, Now);
        var before = c.Version;
        c.AddScope("read", Actor, Now);
        c.Version.Should().Be(before);
    }

    [Fact]
    public void RemoveScope_ShouldIncrementVersion()
    {
        var c = CreateActive();
        c.AddScope("read", Actor, Now);
        var before = c.Version;
        c.RemoveScope("read", Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [Fact]
    public void RemoveScope_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.RemoveScope("read", Actor, Now);
        c.Version.Should().Be(before);
    }

    [Fact]
    public void RotateSecret_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.RotateSecret("v1", SecretRef.Create("key/val"), Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [Fact]
    public void RotateSecret_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        var secret = SecretRef.Create("key/val");
        c.RotateSecret("v1", secret, Actor, Now);
        var before = c.Version;
        c.RotateSecret("v1", secret, Actor, Now);
        c.Version.Should().Be(before);
    }

    [Fact]
    public void Delete_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Delete(Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [Fact]
    public void Delete_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.Delete(Actor, Now);
        var before = c.Version;
        c.Delete(Actor, Now);
        c.Version.Should().Be(before);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var c = CreateActive();
        c.Delete(Actor, Now);
        var before = c.Version;
        c.Restore(Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [Fact]
    public void Restore_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Restore(Actor, Now);
        c.Version.Should().Be(before);
    }
}
