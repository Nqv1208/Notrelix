using FluentAssertions;

namespace Notrelix.Domain.Tests.Maturity;

public class ConcurrencyTests
{
    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow, "second delete");

        workspace.IsDeleted.Should().BeTrue();
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_WhenAlreadyActive_ShouldBeNoOp()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        workspace.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        workspace.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Rename_AfterDelete_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => workspace.Rename("New Name", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
        workspace.Name.Should().Be("My Workspace");
    }

    [Fact]
    public void UpdateSettings_AfterDelete_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => workspace.UpdateSettings(WorkspaceSettings.Create(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Archive_AfterDelete_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => workspace.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }
}
