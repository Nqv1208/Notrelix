using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardViewTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var config = TableViewConfig.Create(JsonValue.EmptyObject());
        var createdBy = Guid.NewGuid();

        var view = BoardView.Create(workspaceId, boardId, "Table View", ViewType.Table, config, createdBy, DateTimeOffset.UtcNow);

        view.Name.Should().Be("Table View");
        view.Type.Should().Be(ViewType.Table);
        view.DomainEvents.Should().ContainSingle(e => e is BoardViewCreatedEvent);
    }

    [Fact]
    public void UpdateConfig_ShouldUpdate_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var config = TableViewConfig.Create(JsonValue.EmptyObject());
        var createdBy = Guid.NewGuid();
        var view = BoardView.Create(workspaceId, boardId, "View", ViewType.Table, config, createdBy, DateTimeOffset.UtcNow);
        view.ClearDomainEvents();

        var newConfig = TableViewConfig.Create(JsonValue.Create("{\"sorts\":[]}"));
        var updatedBy = Guid.NewGuid();

        view.UpdateConfig(newConfig, updatedBy, DateTimeOffset.UtcNow);

        view.Config.Should().Be(newConfig);
        view.DomainEvents.Should().ContainSingle(e => e is BoardViewConfigUpdatedEvent);
    }
}
