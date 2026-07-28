using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Domain.Tests.WorkManagement;

[CoversAggregate(typeof(BoardView))]
public class BoardViewTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var config = TableViewConfig.Create(JsonValue.EmptyObject());
        var createdBy = Guid.NewGuid();

        var view = BoardView.Create(Guid.NewGuid(), workspaceId, boardId, "Table View", ViewType.Table, config, createdBy, DateTimeOffset.UtcNow);

        view.Name.Should().Be("Table View");
        view.Type.Should().Be(ViewType.Table);
        view.DomainEvents.Should().ContainSingle(e => e is BoardViewCreatedDomainEvent);
    }

    [Fact]
    public void UpdateConfig_ShouldUpdate_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var config = TableViewConfig.Create(JsonValue.EmptyObject());
        var createdBy = Guid.NewGuid();
        var view = BoardView.Create(Guid.NewGuid(), workspaceId, boardId, "View", ViewType.Table, config, createdBy, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)view).ClearDomainEvents();

        var newConfig = TableViewConfig.Create(JsonValue.Create("{\"sorts\":[]}"));
        var updatedBy = Guid.NewGuid();

        view.UpdateConfig(newConfig, updatedBy, DateTimeOffset.UtcNow);

        view.Config.Should().Be(newConfig);
        view.DomainEvents.Should().ContainSingle(e => e is BoardViewConfigUpdatedDomainEvent);
    }

    [Fact]
    public void KanbanViewConfig_ShouldReject_EmptyVisibleFieldIds()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":[]}")), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => KanbanViewConfig.Create(field, new[] { Guid.Empty }, Guid.NewGuid());

        act.Should().Throw<BusinessRuleException>().WithMessage("*empty*");
    }

    [Fact]
    public void KanbanViewConfig_ShouldDeduplicate_VisibleFieldIds()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":[]}")), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var config = KanbanViewConfig.Create(field, new[] { id1, id2, id1 }, Guid.NewGuid());

        config.VisibleFieldIds.Should().HaveCount(2);
        config.VisibleFieldIds.Should().Contain(id1);
        config.VisibleFieldIds.Should().Contain(id2);
    }

    [Fact]
    public void KanbanViewConfig_ShouldReject_EmptySwimlaneFieldId()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":[]}")), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => KanbanViewConfig.Create(field, new[] { Guid.NewGuid() }, Guid.Empty);

        act.Should().Throw<BusinessRuleException>().WithMessage("*swimlane*");
    }

    [Fact]
    public void SoftDelete_ShouldSucceed_WhenNotDefaultView()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var view = BoardView.Create(Guid.NewGuid(), workspaceId, boardId, "View", ViewType.Table, TableViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow, isDefault: false);

        view.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        view.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void BoardViewRules_EnsureCanDeleteView_ShouldThrow_WhenDefaultAndOnlyView()
    {
        Action act = () => BoardViewRules.EnsureCanDeleteView(true, 1);

        act.Should().Throw<BusinessRuleException>().WithMessage("*default*");
    }

    [Fact]
    public void BoardViewRules_EnsureCanDeleteView_ShouldNotThrow_WhenMultipleViews()
    {
        Action act = () => BoardViewRules.EnsureCanDeleteView(true, 2);

        act.Should().NotThrow();
    }

    [Fact]
    public void BoardViewRules_EnsureCanDeleteView_ShouldNotThrow_WhenNotDefault()
    {
        Action act = () => BoardViewRules.EnsureCanDeleteView(false, 1);

        act.Should().NotThrow();
    }

    [Fact]
    public void Archive_ShouldSetIsArchived_AndRaiseEvent()
    {
        var view = BoardView.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "View", ViewType.Table, TableViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)view).ClearDomainEvents();

        view.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        view.IsArchived.Should().BeTrue();
        view.DomainEvents.Should().ContainSingle(e => e is BoardViewArchivedDomainEvent);
    }

    [Fact]
    public void Archive_ShouldBeIdempotent()
    {
        var view = BoardView.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "View", ViewType.Table, TableViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        view.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)view).ClearDomainEvents();

        view.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        view.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Archive_ShouldThrow_WhenDeleted()
    {
        var view = BoardView.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "View", ViewType.Table, TableViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        view.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => view.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Unarchive_ShouldClearIsArchived_AndRaiseEvent()
    {
        var view = BoardView.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "View", ViewType.Table, TableViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        view.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)view).ClearDomainEvents();

        view.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        view.IsArchived.Should().BeFalse();
        view.DomainEvents.Should().ContainSingle(e => e is BoardViewUnarchivedDomainEvent);
    }

    [Fact]
    public void Unarchive_ShouldBeIdempotent()
    {
        var view = BoardView.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "View", ViewType.Table, TableViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)view).ClearDomainEvents();

        view.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        view.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_ShouldThrow_WhenDeleted()
    {
        var view = BoardView.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "View", ViewType.Table, TableViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        view.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        view.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => view.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }
}
