using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.Checklists;

namespace Notrelix.Domain.Tests.WorkManagement;

[CoversAggregate(typeof(Checklist))]
public class ChecklistTests
{
    [CoversMutation(typeof(Checklist), nameof(Checklist.Rename), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Create_ShouldUseFractionalIndex()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.Position.Should().Be(FractionalIndex.Create("a0"));
    }

    [CoversMutation(typeof(Checklist), nameof(Checklist.AddItem), MutationScenario.Valid, typeof(string), typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Checklist), nameof(Checklist.RemoveItem), MutationScenario.Valid, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Checklist), nameof(Checklist.ToggleItem), MutationScenario.Valid, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void AddItem_ShouldUseFractionalIndex()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.AddItem("Item 1", FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.Items.Should().HaveCount(1);
        checklist.Items.First().Title.Should().Be("Item 1");
        checklist.Items.First().Position.Should().Be(FractionalIndex.Create("a1"));
    }

    [CoversMutation(typeof(Checklist), nameof(Checklist.AddItem), MutationScenario.Valid, typeof(string), typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Checklist), nameof(Checklist.UpdatePosition), MutationScenario.Valid, typeof(FractionalIndex), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Checklist), nameof(Checklist.RemoveItem), MutationScenario.Valid, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Checklist), nameof(Checklist.ToggleItem), MutationScenario.Valid, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void AddItem_ShouldMaintainPositionOrder_WithFractionalIndex()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.AddItem("First", FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        checklist.AddItem("Between", FractionalIndex.Create("a2"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        checklist.AddItem("Last", FractionalIndex.Create("a3"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        checklist.Items.Should().HaveCount(3);
        var ordered = checklist.Items.OrderBy(i => i.Position).ToList();
        ordered[0].Title.Should().Be("First");
        ordered[1].Title.Should().Be("Between");
        ordered[2].Title.Should().Be("Last");
    }
}
