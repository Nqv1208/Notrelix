using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class SecretRotationBoundaryTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void RotateSecret_ShouldSetCurrentSecretState()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/google-api-key");

        connection.RotateSecret("v1", secretRef, Actor, Now);

        connection.CurrentSecretVersion.Should().Be("v1");
        connection.CurrentSecretRef.Should().Be(secretRef);
    }

    [Fact]
    public void RotateSecret_ShouldSetSecretRotatedAt()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/key");

        connection.RotateSecret("v1", secretRef, Actor, Now);

        connection.SecretRotatedAt.Should().Be(Now);
    }

    [Fact]
    public void RotateSecret_ShouldRaiseEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/key");

        connection.RotateSecret("v1", secretRef, Actor, Now);

        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationSecretRotatedDomainEvent);
        var @event = connection.DomainEvents.OfType<IntegrationSecretRotatedDomainEvent>().Single();
        @event.Version.Should().Be("v1");
    }

    [Fact]
    public void RotateSecret_WhenSameVersionAndRef_ShouldBeNoOp()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/key");
        connection.RotateSecret("v1", secretRef, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.RotateSecret("v1", secretRef, Actor, Now);

        connection.DomainEvents.Should().BeEmpty();
        connection.SecretRotatedAt.Should().Be(Now);
    }

    [Fact]
    public void RotateSecret_WhenDifferentVersion_ShouldRaiseEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/key");
        connection.RotateSecret("v1", secretRef, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.RotateSecret("v2", secretRef, Actor, Now);

        connection.CurrentSecretVersion.Should().Be("v2");
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationSecretRotatedDomainEvent);
    }

    [Fact]
    public void RotateSecret_WhenDifferentRef_ShouldRaiseEvent()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var ref1 = SecretRef.Create("arn:aws:secrets:::secret/key-v1");
        var ref2 = SecretRef.Create("arn:aws:secrets:::secret/key-v2");
        connection.RotateSecret("v1", ref1, Actor, Now);
        ((IHasDomainEvents)connection).ClearDomainEvents();

        connection.RotateSecret("v1", ref2, Actor, Now);

        connection.CurrentSecretRef.Should().Be(ref2);
        connection.DomainEvents.Should().ContainSingle(e => e is IntegrationSecretRotatedDomainEvent);
    }

    [Fact]
    public void RotateSecret_ShouldTrimVersion()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/key");

        connection.RotateSecret("  v1  ", secretRef, Actor, Now);

        connection.CurrentSecretVersion.Should().Be("v1");
    }

    [Fact]
    public void RotateSecret_EmptyVersion_ShouldThrow()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/key");

        var act = () => connection.RotateSecret("", secretRef, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RotateSecret_NullSecretRef_ShouldThrow()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);

        var act = () => connection.RotateSecret("v1", null!, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RotateSecret_ShouldIncrementVersion()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/key");
        var initialVersion = connection.Version;

        connection.RotateSecret("v1", secretRef, Actor, Now);

        connection.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void RotateSecret_ShouldUpdateAuditFields()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/key");

        var rotatedAt = Now.AddMinutes(5);
        var rotator = Guid.NewGuid();
        connection.RotateSecret("v1", secretRef, rotator, rotatedAt);

        connection.UpdatedBy.Should().Be(rotator);
        connection.UpdatedAt.Should().Be(rotatedAt);
    }

    [Fact]
    public void RotateSecret_WhenDeleted_ShouldThrow()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        connection.Delete(Actor, Now);
        var secretRef = SecretRef.Create("arn:aws:secrets:::secret/key");

        var act = () => connection.RotateSecret("v1", secretRef, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MultipleRotations_ShouldTrackLatestState()
    {
        var connection = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var ref1 = SecretRef.Create("arn:aws:secrets:::secret/key-v1");
        var ref2 = SecretRef.Create("arn:aws:secrets:::secret/key-v2");
        var ref3 = SecretRef.Create("arn:aws:secrets:::secret/key-v3");

        connection.RotateSecret("v1", ref1, Actor, Now);
        connection.RotateSecret("v2", ref2, Actor, Now.AddMinutes(1));
        connection.RotateSecret("v3", ref3, Actor, Now.AddMinutes(2));

        connection.CurrentSecretVersion.Should().Be("v3");
        connection.CurrentSecretRef.Should().Be(ref3);
        connection.SecretRotatedAt.Should().Be(Now.AddMinutes(2));
    }
}
