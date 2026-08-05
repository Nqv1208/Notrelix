using FluentAssertions;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceTenantOwnershipTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public void Workspace_AccountId_ShouldBeReadOnly()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "Test", "test", DateTimeOffset.UtcNow);

        var accountProp = typeof(Workspace).GetProperty("AccountId");
        accountProp.Should().NotBeNull();
        var setter = accountProp!.GetSetMethod(nonPublic: true);
        setter.Should().NotBeNull("AccountId has a private setter for EF Core");
        setter!.IsPrivate.Should().BeTrue("AccountId setter should be private");
    }

    [Fact]
    public void Workspace_ShouldNotExposeMethodToChangeAccountId()
    {
        var workspaceType = typeof(Workspace);

        var updateMethod = workspaceType.GetMethod("UpdateAccountId");
        updateMethod.Should().BeNull("UpdateAccountId should not exist — AccountId is immutable after construction");
    }

    [Fact]
    public void Workspace_Create_ShouldSetAccountId()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "Test", "test", DateTimeOffset.UtcNow);

        workspace.AccountId.Should().Be(AccountId);
    }

    [Fact]
    public void Workspace_ShouldNotImplementIWorkspaceScoped()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "Test", "test", DateTimeOffset.UtcNow);

        workspace.Should().NotBeAssignableTo<IWorkspaceScoped>();
    }
}
