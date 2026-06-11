using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
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
        var position = FractionalIndex.Create("a0");

        var field = BoardField.Create(workspaceId, boardId, "Due Date", FieldType.Date, settings, position, createdBy, DateTimeOffset.UtcNow);

        field.Name.Should().Be("Due Date");
        field.Type.Should().Be(FieldType.Date);
        field.Settings.Should().Be(settings);
        field.DomainEvents.Should().ContainSingle(e => e is BoardFieldCreatedEvent);
    }

    [Fact]
    public void AddOption_ShouldSucceed_AndRaiseEvent()
    {
        var settings = FieldSettings.Create(JsonValue.EmptyObject());
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, settings, position, Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.ClearDomainEvents();

        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("b0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        field.Options.Should().HaveCount(1);
        field.Options.First().Name.Should().Be("Done");
        field.DomainEvents.Should().ContainSingle(e => e is FieldOptionAddedEvent);
    }

    [Fact]
    public void AddOption_ShouldThrow_WhenDuplicateName()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("b0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => field.AddOption("Done", Color.Create("#FF0000"), FractionalIndex.Create("c0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*duplicate*");
    }

    [Fact]
    public void AddOption_ShouldAllow_WhenSameNameDifferentCase()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("b0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => field.AddOption("done", Color.Create("#FF0000"), FractionalIndex.Create("c0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*duplicate*");
    }
}
