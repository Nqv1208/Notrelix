using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations.Connections;

public class IntegrationScopeCanonicalizationTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void AddScope_ShouldTrim()
    {
        var c = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        c.AddScope("  read  ", Actor, Now);
        c.Scopes.Should().ContainSingle(s => s.Scope == "read");
    }

    [Fact]
    public void AddScope_WithEmpty_ShouldThrow()
    {
        var c = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var act = () => c.AddScope("", Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void AddScope_WithWhitespace_ShouldThrow()
    {
        var c = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var act = () => c.AddScope("   ", Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void AddScope_CaseSensitive_ShouldTreatDifferentAsNonDuplicate()
    {
        var c = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        c.AddScope("Read", Actor, Now);
        c.AddScope("read", Actor, Now);
        c.Scopes.Should().HaveCount(2);
    }

    [Fact]
    public void AddScope_ExactSame_ShouldBeNoOp()
    {
        var c = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        c.AddScope("read", Actor, Now);
        var before = c.Scopes.Count;
        c.AddScope("read", Actor, Now);
        c.Scopes.Should().HaveCount(before);
    }

    [Fact]
    public void RemoveScope_ShouldTrim()
    {
        var c = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        c.AddScope("read", Actor, Now);
        c.RemoveScope("  read  ", Actor, Now);
        c.Scopes.Should().BeEmpty();
    }

    [Fact]
    public void RemoveScope_WithEmpty_ShouldThrow()
    {
        var c = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var act = () => c.RemoveScope("", Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RemoveScope_WhenNotFound_ShouldBeNoOp()
    {
        var c = IntegrationConnection.Create(AccountId, WorkspaceId, IntegrationProvider.Google, Actor, Now);
        var before = c.Scopes.Count;
        c.RemoveScope("nonexistent", Actor, Now);
        c.Scopes.Should().HaveCount(before);
    }
}
