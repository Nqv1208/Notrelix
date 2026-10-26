using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Items.Events;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemOwnersRemindedTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    private static BoardItem NewItem()
    {
        var item = BoardItem.CreateRoot(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item",
            FractionalIndex.Create("a0"), Guid.NewGuid(), Now);
        return item;
    }

    private static List<BoardItemOwnersRemindedDomainEvent> Reminded(BoardItem item) =>
        item.DomainEvents.OfType<BoardItemOwnersRemindedDomainEvent>().ToList();

    [Fact]
    public void RecordOwnersReminded_RecordsTheFactForEveryoneButTheSender_AndLeavesTheItemUnchanged()
    {
        var item = NewItem();
        var version = item.Version;
        var sender = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        item.RecordOwnersReminded(sender, [a, b, sender, a], Now);

        var fact = Reminded(item).Should().ContainSingle().Which;
        fact.ItemId.Should().Be(item.Id);
        fact.BoardId.Should().Be(item.BoardId);
        fact.SenderUserId.Should().Be(sender);
        fact.RecipientUserIds.Should().BeEquivalentTo([a, b]);
        fact.OccurredAt.Should().Be(Now);
        item.Version.Should().Be(version);
    }

    [Fact]
    public void RecordOwnersReminded_Rejects_WhenNobodyElseIsInCharge_AndRecordsNothing()
    {
        var item = NewItem();
        var sender = Guid.NewGuid();

        var act = () => item.RecordOwnersReminded(sender, [sender], Now);

        act.Should().Throw<BusinessRuleException>();
        Reminded(item).Should().BeEmpty();
    }

    [Fact]
    public void RecordOwnersReminded_Rejects_AnArchivedItem()
    {
        var item = NewItem();
        item.Archive(Guid.NewGuid(), Now);

        var act = () => item.RecordOwnersReminded(Guid.NewGuid(), [Guid.NewGuid()], Now);

        act.Should().Throw<DomainException>();
        Reminded(item).Should().BeEmpty();
    }
}
