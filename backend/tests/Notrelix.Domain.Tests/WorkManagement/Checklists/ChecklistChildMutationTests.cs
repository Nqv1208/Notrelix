using FluentAssertions;
using Notrelix.Domain.WorkManagement.Checklists;

namespace Notrelix.Domain.Tests.WorkManagement.Checklists;

/// <summary>
/// WM-V3-APP-CHK-001: every ChecklistItem mutation runs through the Checklist
/// aggregate, which owns the version, audit and events of the child change.
/// </summary>
public class ChecklistChildMutationTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private (Checklist Checklist, ChecklistItem Item) CreateWithItem()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Launch", FractionalIndex.Create("a0"), _actorId, _now);
        var item = checklist.AddItem("Write notes", FractionalIndex.Create("a1"), _actorId, _now);
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        return (checklist, item);
    }

    [Fact]
    public void SetItemCompletion_ToDone_ChecksTheItem_WithOneVersionAndEvent()
    {
        var (checklist, item) = CreateWithItem();
        var version = checklist.Version;

        checklist.SetItemCompletion(item.Id, true, _actorId, _now);

        item.Status.Should().Be(ChecklistItemStatus.Done);
        item.CompletedAt.Should().Be(_now);
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ChecklistItemToggledDomainEvent>()
            .Which.IsDone.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetItemCompletion_ToTheCurrentState_IsASemanticNoOp(bool startDone)
    {
        var (checklist, item) = CreateWithItem();
        if (startDone) checklist.SetItemCompletion(item.Id, true, _actorId, _now);
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        var version = checklist.Version;

        checklist.SetItemCompletion(item.Id, startDone, _actorId, _now.AddMinutes(1));

        item.IsDone.Should().Be(startDone, "a repeated desired state never inverts the item");
        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ToggleItem_StillInvertsTheCurrentState()
    {
        var (checklist, item) = CreateWithItem();

        checklist.ToggleItem(item.Id, _actorId, _now);
        checklist.ToggleItem(item.Id, _actorId, _now);

        item.IsDone.Should().BeFalse();
        item.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void RenameItem_TrimsTitle_WithOneVersionAndEvent()
    {
        var (checklist, item) = CreateWithItem();
        var version = checklist.Version;

        checklist.RenameItem(item.Id, "  Publish notes ", _actorId, _now);

        item.Title.Should().Be("Publish notes");
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ChecklistItemRenamedDomainEvent>()
            .Which.Title.Should().Be("Publish notes");
    }

    [Fact]
    public void RenameItem_ToTheSameNormalizedTitle_IsASemanticNoOp()
    {
        var (checklist, item) = CreateWithItem();
        var version = checklist.Version;

        checklist.RenameItem(item.Id, " Write notes ", _actorId, _now);

        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RenameItem_ToABlankTitle_IsRejected_AndChangesNothing(string title)
    {
        var (checklist, item) = CreateWithItem();
        var version = checklist.Version;

        var act = () => checklist.RenameItem(item.Id, title, _actorId, _now);

        act.Should().Throw<Exception>();
        item.Title.Should().Be("Write notes");
        checklist.Version.Should().Be(version);
    }

    [Fact]
    public void RenameItem_BeyondTheMaximumLength_IsRejected()
    {
        var (checklist, item) = CreateWithItem();

        var act = () => checklist.RenameItem(item.Id, new string('x', Checklist.MaxTitleLength + 1), _actorId, _now);

        act.Should().Throw<Exception>();
        item.Title.Should().Be("Write notes");
    }

    [Fact]
    public void MoveItem_RewritesOnlyTheMovedKey_WithOneEvent()
    {
        var (checklist, item) = CreateWithItem();
        var other = checklist.AddItem("Review", FractionalIndex.Create("a3"), _actorId, _now);
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        var version = checklist.Version;

        checklist.MoveItem(item.Id, FractionalIndex.Create("a5"), _actorId, _now);

        item.Position.Value.Should().Be("a5");
        other.Position.Value.Should().Be("a3");
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ChecklistItemMovedDomainEvent>();
    }

    [Fact]
    public void MoveItem_ToItsCurrentKey_IsASemanticNoOp()
    {
        var (checklist, item) = CreateWithItem();
        var version = checklist.Version;

        checklist.MoveItem(item.Id, FractionalIndex.Create("a1"), _actorId, _now);

        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChildMutation_OfAnUnknownItem_IsRejected_AndChangesNothing()
    {
        var (checklist, _) = CreateWithItem();
        var version = checklist.Version;
        var unknown = Guid.NewGuid();

        new Action[]
        {
            () => checklist.SetItemCompletion(unknown, true, _actorId, _now),
            () => checklist.RenameItem(unknown, "x", _actorId, _now),
            () => checklist.MoveItem(unknown, FractionalIndex.Create("a5"), _actorId, _now),
        }.Should().AllSatisfy(act => act.Should().Throw<BusinessRuleException>());

        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChildMutation_OnADeletedChecklist_IsRejected()
    {
        var (checklist, item) = CreateWithItem();
        checklist.Delete(_actorId, _now);

        var act = () => checklist.SetItemCompletion(item.Id, true, _actorId, _now);

        act.Should().Throw<Exception>();
        item.IsDone.Should().BeFalse();
    }

    [Fact]
    public void RenameAndMoveOfTheChecklist_RaiseTheirFacts()
    {
        var (checklist, _) = CreateWithItem();

        checklist.Rename("Release", _actorId, _now);
        checklist.UpdatePosition(FractionalIndex.Create("a7"), _actorId, _now);

        checklist.DomainEvents.Select(e => e.GetType()).Should().Equal(typeof(ChecklistRenamedDomainEvent), typeof(ChecklistMovedDomainEvent));
    }
}
