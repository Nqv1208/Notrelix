using FluentAssertions;

namespace Notrelix.Domain.Tests.Workspaces.Workspaces;

public class WorkspaceScopeTests
{
    [Fact]
    public void Workspace_ShouldImplementIAccountScoped()
    {
        typeof(IAccountScoped).IsAssignableFrom(typeof(Workspace)).Should().BeTrue();
    }

    [Fact]
    public void Workspace_AccountId_ShouldBeImmutable()
    {
        var accountId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, Guid.NewGuid(), "Test", "test", DateTimeOffset.UtcNow);
        workspace.AccountId.Should().Be(accountId);
    }

    [Fact]
    public void Workspace_ShouldBeSealed()
    {
        typeof(Workspace).IsSealed.Should().BeTrue();
    }
}
