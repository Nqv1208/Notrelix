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

    [CoversMutation(typeof(IntegrationConnection), "MarkError(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void MarkError_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.MarkError("err", Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), "MarkError(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void MarkError_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.MarkError("err", Actor, Now);
        var before = c.Version;
        c.MarkError("err", Actor, Now);
        c.Version.Should().Be(before);
    }

    [CoversMutation(typeof(IntegrationConnection), "Reconnect(System.String,System.DateTimeOffset?,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Reconnect_ShouldIncrementVersion()
    {
        var c = CreateActive();
        c.MarkError("err", Actor, Now);
        var before = c.Version;
        c.Reconnect(null, null, Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), "Reconnect(System.String,System.DateTimeOffset?,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Reconnect_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Reconnect(null, null, Actor, Now);
        c.Version.Should().Be(before);
    }

    [CoversMutation(typeof(IntegrationConnection), "Disconnect(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Disconnect_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Disconnect(Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), "Disconnect(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Disconnect_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.Disconnect(Actor, Now);
        var before = c.Version;
        c.Disconnect(Actor, Now);
        c.Version.Should().Be(before);
    }

    [CoversMutation(typeof(IntegrationConnection), "AddScope(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Scope)]
    [Fact]
    public void AddScope_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.AddScope("read", Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), "AddScope(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
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

    [CoversMutation(typeof(IntegrationConnection), "RotateSecret(System.String,Notrelix.Domain.SharedKernel.SecretRef,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void RotateSecret_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.RotateSecret("v1", SecretRef.Create("key/val"), Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), "RotateSecret(System.String,Notrelix.Domain.SharedKernel.SecretRef,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
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

    [CoversMutation(typeof(IntegrationConnection), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.SoftDelete(Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void SoftDelete_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        c.SoftDelete(Actor, Now);
        var before = c.Version;
        c.SoftDelete(Actor, Now);
        c.Version.Should().Be(before);
    }

    [CoversMutation(typeof(IntegrationConnection), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var c = CreateActive();
        c.SoftDelete(Actor, Now);
        var before = c.Version;
        c.Restore(Actor, Now);
        c.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(IntegrationConnection), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Restore_NoOp_ShouldNotIncrementVersion()
    {
        var c = CreateActive();
        var before = c.Version;
        c.Restore(Actor, Now);
        c.Version.Should().Be(before);
    }
}
