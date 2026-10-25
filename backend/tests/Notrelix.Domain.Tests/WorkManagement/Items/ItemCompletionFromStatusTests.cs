using FluentAssertions;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

/// <summary>
/// Moving an Item into a Done-category status completes it; moving it out of the
/// done category, or clearing its status, reopens it. Staying on the same side is
/// a semantic no-op.
/// </summary>
public class ItemCompletionFromStatusTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 3, 0, 0, TimeSpan.Zero);

    private static Notrelix.Domain.WorkManagement.Items.BoardItem Item() =>
        Notrelix.Domain.WorkManagement.Items.BoardItem.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item",
            FractionalIndex.Create("a0"), Actor, Now, "1", 1);

    [Fact]
    public void ADoneCategory_CompletesTheItem_AtTheChangeTime()
    {
        var item = Item();

        item.SyncCompletionWithStatus(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory.Done, Actor, Now.AddMinutes(5));

        item.CompletedAt.Should().Be(Now.AddMinutes(5));
        item.DomainEvents.Should().Contain(e => e is BoardItemCompletedDomainEvent);
    }

    [Fact]
    public void StayingDone_KeepsTheOriginalCompletionTime()
    {
        var item = Item();
        item.SyncCompletionWithStatus(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory.Done, Actor, Now.AddMinutes(5));
        var version = item.Version;

        item.SyncCompletionWithStatus(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory.Done, Actor, Now.AddMinutes(9));

        item.CompletedAt.Should().Be(Now.AddMinutes(5));
        item.Version.Should().Be(version);
    }

    [Theory]
    [InlineData(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory.Todo)]
    [InlineData(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory.InProgress)]
    [InlineData(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory.Review)]
    [InlineData(null)]
    public void LeavingTheDoneCategory_ReopensTheItem(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory? category)
    {
        var item = Item();
        item.SyncCompletionWithStatus(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory.Done, Actor, Now.AddMinutes(5));

        item.SyncCompletionWithStatus(category, Actor, Now.AddMinutes(6));

        item.CompletedAt.Should().BeNull();
        item.DomainEvents.Should().Contain(e => e is BoardItemReopenedDomainEvent);
    }

    [Fact]
    public void ANotDoneCategory_OnAnOpenItem_IsANoOp()
    {
        var item = Item();
        var version = item.Version;

        item.SyncCompletionWithStatus(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory.InProgress, Actor, Now.AddMinutes(5));

        item.CompletedAt.Should().BeNull();
        item.Version.Should().Be(version);
    }

    [Fact]
    public void Completing_RecordsWhoAndWhichStatusItLeft_AndReopeningClearsThem()
    {
        var item = Item();
        var before = Guid.NewGuid();

        item.SyncCompletionWithStatus(Notrelix.Domain.WorkManagement.Fields.WorkStatusCategory.Done, Actor, Now.AddMinutes(5), before);

        item.CompletedBy.Should().Be(Actor);
        item.StatusBeforeCompletionOptionId.Should().Be(before);

        item.Reopen(Actor, Now.AddMinutes(6));

        item.CompletedBy.Should().BeNull();
        item.StatusBeforeCompletionOptionId.Should().BeNull();
    }

    [Fact]
    public void Archiving_RecordsWhoAndWhen_AndUnarchivingClearsThem()
    {
        var item = Item();

        item.Archive(Actor, Now.AddMinutes(5));

        item.ArchivedBy.Should().Be(Actor);
        item.ArchivedAt.Should().Be(Now.AddMinutes(5));

        item.Unarchive(Actor, Now.AddMinutes(6));

        item.ArchivedBy.Should().BeNull();
        item.ArchivedAt.Should().BeNull();
    }
}
