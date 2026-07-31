using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class IntegrationSecretStateTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static IntegrationConnection CreateActive() =>
        IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Slack, Actor, Now);

    [Fact]
    public void Create_ShouldHaveNullSecretState()
    {
        var c = CreateActive();
        c.CurrentSecretVersion.Should().BeNull();
        c.CurrentSecretRef.Should().BeNull();
        c.SecretRotatedAt.Should().BeNull();
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.RotateSecret), MutationScenario.Valid, typeof(string), typeof(SecretRef), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_ShouldSetSecretState()
    {
        var c = CreateActive();
        var secretRef = SecretRef.Create("projects/p/secrets/s/versions/1");
        c.RotateSecret("v1", secretRef, Actor, Now);
        c.CurrentSecretVersion.Should().Be("v1");
        c.CurrentSecretRef.Should().Be(secretRef);
        c.SecretRotatedAt.Should().Be(Now);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.RotateSecret), MutationScenario.Version, typeof(string), typeof(SecretRef), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_WithNewVersion_ShouldUpdate()
    {
        var c = CreateActive();
        c.RotateSecret("v1", SecretRef.Create("key/val"), Actor, Now);
        var newRef = SecretRef.Create("key/val2");
        c.RotateSecret("v2", newRef, Actor, Now);
        c.CurrentSecretVersion.Should().Be("v2");
        c.CurrentSecretRef.Should().Be(newRef);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.RotateSecret), MutationScenario.NoOp, typeof(string), typeof(SecretRef), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_WithSameState_ShouldBeNoOp()
    {
        var c = CreateActive();
        var secretRef = SecretRef.Create("key/val");
        c.RotateSecret("v1", secretRef, Actor, Now);
        c.RotateSecret("v1", secretRef, Actor, Now);
        c.DomainEvents.Should().ContainSingle(e => e is IntegrationSecretRotatedDomainEvent);
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.RotateSecret), MutationScenario.Invalid, typeof(string), typeof(SecretRef), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_WithEmptyVersion_ShouldThrow()
    {
        var c = CreateActive();
        var act = () => c.RotateSecret("", SecretRef.Create("key/val"), Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.RotateSecret), MutationScenario.Invalid, typeof(string), typeof(SecretRef), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_WithNullRef_ShouldThrow()
    {
        var c = CreateActive();
        var act = () => c.RotateSecret("v1", null!, Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(IntegrationConnection), nameof(IntegrationConnection.RotateSecret), MutationScenario.Invalid, typeof(string), typeof(SecretRef), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateSecret_WhenDeleted_ShouldThrow()
    {
        var c = CreateActive();
        c.Delete(Actor, Now);
        var act = () => c.RotateSecret("v1", SecretRef.Create("key/val"), Actor, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SecretRef_Equality_ByValue()
    {
        var a = SecretRef.Create("key/val");
        var b = SecretRef.Create("key/val");
        var c = SecretRef.Create("key/other");
        a.Should().Be(b);
        a.Should().NotBe(c);
    }
}
