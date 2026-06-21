using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces.Workspaces;
using Xunit;

namespace Notrelix.Domain.Tests.Maturity;

public class ConcurrencyTests
{
    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        workspace.ClearDomainEvents();

        workspace.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow, "second delete");

        workspace.IsDeleted.Should().BeTrue();
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_WhenAlreadyActive_ShouldBeNoOp()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        workspace.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        workspace.IsDeleted.Should().BeFalse();
        workspace.Status.Should().Be(WorkspaceStatus.Active);
    }

    [Fact]
    public void Rename_AfterSoftDelete_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => workspace.Rename("New Name", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
        workspace.Name.Should().Be("My Workspace");
    }

    [Fact]
    public void UpdateSettings_AfterSoftDelete_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => workspace.UpdateSettings(WorkspaceSettings.Create(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Archive_AfterSoftDelete_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => workspace.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }
}
