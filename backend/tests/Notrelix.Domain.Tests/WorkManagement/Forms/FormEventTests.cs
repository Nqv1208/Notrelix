using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Domain.Tests.WorkManagement.Forms;

public class FormEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Form_UpdateDetails_ShouldRaiseEvent()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.UpdateDetails("Updated Form", BoardVisibility.Workspace, "{}", "{}", Actor, Now);

        form.Version.Should().Be(version + 1);
        form.DomainEvents.Should().ContainSingle(e => e is FormDetailsUpdatedDomainEvent);
        var evt = (FormDetailsUpdatedDomainEvent)form.DomainEvents.Single(e => e is FormDetailsUpdatedDomainEvent);
        evt.Name.Should().Be("Updated Form");
    }

    [Fact]
    public void Form_SoftDelete_ShouldRaiseEvent()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.SoftDelete(Actor, Now);

        form.IsDeleted.Should().BeTrue();
        form.Version.Should().Be(version + 1);
        form.DomainEvents.Should().ContainSingle(e => e is FormSoftDeletedDomainEvent);
    }

    [Fact]
    public void Form_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.SoftDelete(Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.SoftDelete(Actor, Now);

        form.Version.Should().Be(version);
        form.DomainEvents.Should().NotContain(e => e is FormSoftDeletedDomainEvent);
    }

    [Fact]
    public void Form_Restore_ShouldRaiseEvent()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.SoftDelete(Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.Restore(Actor, Now);

        form.IsDeleted.Should().BeFalse();
        form.Version.Should().Be(version + 1);
        form.DomainEvents.Should().ContainSingle(e => e is FormRestoredDomainEvent);
    }

    [Fact]
    public void Form_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.Restore(Actor, Now);

        form.Version.Should().Be(version);
        form.DomainEvents.Should().NotContain(e => e is FormRestoredDomainEvent);
    }
}
