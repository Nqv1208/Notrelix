using FluentAssertions;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemArchivedTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid GroupA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static BoardItem CreateArchivedItem()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item",
            FractionalIndex.Create("a0"), Actor, Now);
        item.Archive(Actor, Now);
        return item;
    }

    [CoversMutation(typeof(BoardItem), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ArchivedItem_Rename_ShouldThrow()
    {
        var item = CreateArchivedItem();
        var act = () => item.Rename("New Name", Actor, Now);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*archived*");
    }

    [CoversMutation(typeof(BoardItem), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ArchivedItem_MoveToGroup_ShouldThrow()
    {
        var item = CreateArchivedItem();
        var groupRef = new BoardGroupRef(AccountId, WsA, BoardA, Guid.NewGuid());
        var act = () => item.MoveToGroup(groupRef, FractionalIndex.Create("a1"), Actor, Now);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*archived*");
    }

    [CoversMutation(typeof(BoardItem), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ArchivedItem_UpdateFieldValue_ShouldThrow()
    {
        var item = CreateArchivedItem();
        var field = BoardField.Create(AccountId, WsA, BoardA, "Field", FieldType.Text,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), Actor, Now);
        var act = () => item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create("\"test\"")), Actor, Now);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*archived*");
    }

    [CoversMutation(typeof(BoardItem), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ArchivedItem_SetTimeline_ShouldThrow()
    {
        var item = CreateArchivedItem();
        var act = () => item.SetTimeline(Now, Now.AddDays(7), Actor, Now);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*archived*");
    }

    [CoversMutation(typeof(BoardItem), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ArchivedItem_Complete_ShouldThrow()
    {
        var item = CreateArchivedItem();
        var act = () => item.Complete(Now, Actor, Now);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*archived*");
    }

    [CoversMutation(typeof(BoardItem), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ArchivedItem_AssignParentItem_ShouldThrow()
    {
        var item = CreateArchivedItem();
        var act = () => item.AssignParentItem(null, 0, new Dictionary<Guid, ItemParentSnapshot>(), Actor, Now);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*archived*");
    }

    [Fact]
    public void FormulaField_ManualWrite_ShouldThrow()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item",
            FractionalIndex.Create("a0"), Actor, Now);
        var field = BoardField.Create(AccountId, WsA, BoardA, "Formula", FieldType.Formula,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), Actor, Now);

        var act = () => item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create("\"result\"")), Actor, Now);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*computed*");
    }

    [Fact]
    public void RollupField_ManualWrite_ShouldThrow()
    {
        var item = BoardItem.Create(AccountId, WsA, BoardA, GroupA, "Item",
            FractionalIndex.Create("a0"), Actor, Now);
        var field = BoardField.Create(AccountId, WsA, BoardA, "Rollup", FieldType.Rollup,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), Actor, Now);

        var act = () => item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create("\"result\"")), Actor, Now);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*computed*");
    }
}
