using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardFieldTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var settings = FieldSettings.Create(JsonValue.Create("{\"required\":true}"));
        var position = FractionalIndex.Create(1.0);

        var field = BoardField.Create(workspaceId, boardId, "Due Date", FieldType.Date, settings, position, createdBy);

        field.Name.Should().Be("Due Date");
        field.Type.Should().Be(FieldType.Date);
        field.Settings.Should().Be(settings);
        field.DomainEvents.Should().ContainSingle(e => e is BoardFieldCreatedEvent);
    }

    [Fact]
    public void AddOption_ShouldSucceed_AndRaiseEvent()
    {
        var settings = FieldSettings.Create(JsonValue.EmptyObject());
        var position = FractionalIndex.Create(1.0);
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, settings, position, Guid.NewGuid());
        field.ClearDomainEvents();

        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create(2.0), Guid.NewGuid());

        field.Options.Should().HaveCount(1);
        field.Options.First().Name.Should().Be("Done");
        field.DomainEvents.Should().ContainSingle(e => e is FieldOptionAddedEvent);
    }
}
