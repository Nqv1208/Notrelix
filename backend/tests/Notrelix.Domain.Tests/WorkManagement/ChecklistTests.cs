using FluentAssertions;
using Notrelix.Domain.WorkManagement.Checklists;

namespace Notrelix.Domain.Tests.WorkManagement;

public class ChecklistTests
{
    [Fact]
    public void Create_ShouldUseFractionalIndex()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.Position.Should().Be(FractionalIndex.Create("a0"));
    }

    [Fact]
    public void AddItem_ShouldUseFractionalIndex()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.AddItem("Item 1", FractionalIndex.Create("b0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.Items.Should().HaveCount(1);
        checklist.Items.First().Title.Should().Be("Item 1");
        checklist.Items.First().Position.Should().Be(FractionalIndex.Create("b0"));
    }

    [Fact]
    public void AddItem_ShouldMaintainPositionOrder_WithFractionalIndex()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.AddItem("First", FractionalIndex.Create("b0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        checklist.AddItem("Between", FractionalIndex.Create("b1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        checklist.AddItem("Last", FractionalIndex.Create("c0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.Items.Should().HaveCount(3);
        var ordered = checklist.Items.OrderBy(i => i.Position).ToList();
        ordered[0].Title.Should().Be("First");
        ordered[1].Title.Should().Be("Between");
        ordered[2].Title.Should().Be("Last");
    }
}
