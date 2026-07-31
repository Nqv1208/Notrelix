using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class IntegrationConnectionMutationAtomicityTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static IntegrationConnection CreateActive() =>
        IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.MarkError), MutationScenario.Version, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MarkError_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.MarkError("err", Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.MarkError), MutationScenario.NoOp, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MarkError_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.MarkError("err", Actor, Now);
        var before = c.Version;
        c.MarkError("err", Actor, Now);
        c.Version.Should().Be(before);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Reconnect), MutationScenario.Version, typeof(string), typeof(DateTimeOffset?), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Reconnect_ShouldIncrementVersion()
    {
        var c = CreateActive();
        c.MarkError("err", Actor, Now);
        var before = c.Version;
        c.Reconnect(null, null, Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Reconnect), MutationScenario.NoOp, typeof(string), typeof(DateTimeOffset?), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Reconnect_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Reconnect(null, null, Actor, Now);
        c.Version.Should().Be(before);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Disconnect), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Disconnect_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Disconnect(Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Disconnect), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Disconnect_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.Disconnect(Actor, Now);
        var before = c.Version;
        c.Disconnect(Actor, Now);
        c.Version.Should().Be(before);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.AddScope), MutationScenario.Scope, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void AddScope_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.AddScope("read", Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.AddScope), MutationScenario.NoOp, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.RotateSecret), MutationScenario.Version, typeof(string), typeof(SecretRef), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.RotateSecret("v1", SecretRef.Create("key/val"), Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.RotateSecret), MutationScenario.NoOp, typeof(string), typeof(SecretRef), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Delete(Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Delete), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.Delete(Actor, Now);
        var before = c.Version;
        c.Delete(Actor, Now);
        c.Version.Should().Be(before);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var c = CreateActive();
        c.Delete(Actor, Now);
        var before = c.Version;
        c.Restore(Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.Restore), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Restore(Actor, Now);
        c.Version.Should().Be(before);
    }
}
