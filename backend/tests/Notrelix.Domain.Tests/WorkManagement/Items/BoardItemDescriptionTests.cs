using FluentAssertions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardItemDescriptionTests
{
    private static BoardItem NewItem(string? description = null, DateTimeOffset? start = null, DateTimeOffset? due = null) =>
        BoardItem.CreateRoot(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item",
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow,
            startedAt: start, dueAt: due, descriptionMd: description);

    [Fact]
    public void CreateRoot_KeepsTheDescriptionAndDates_GivenAtCreation()
    {
        var start = DateTimeOffset.UtcNow;
        var item = NewItem("Plan the **move**", start, start.AddDays(3));

        item.DescriptionMd.Should().Be("Plan the **move**");
        item.StartedAt.Should().Be(start);
        item.DueAt.Should().Be(start.AddDays(3));
    }

    [Fact]
    public void CreateRoot_Rejects_ADueDateBeforeTheStartDate()
    {
        var start = DateTimeOffset.UtcNow;

        var act = () => NewItem(null, start, start.AddDays(-1));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Item_DueDateMustBeAfterStartDate);
    }

    [Fact]
    public void CreateRoot_Rejects_ADescriptionOverTheLimit()
    {
        var act = () => NewItem(new string('x', BoardItem.DescriptionMaxLength + 1));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Item_DescriptionTooLong);
    }

    [Fact]
    public void SetDescription_ChangesTheText_AndTheVersion()
    {
        var item = NewItem();
        var version = item.Version;
        var by = Guid.NewGuid();

        item.SetDescription("First line\r\nSecond line  ", by, DateTimeOffset.UtcNow);

        item.DescriptionMd.Should().Be("First line\nSecond line");
        item.Version.Should().Be(version + 1);
        item.UpdatedBy.Should().Be(by);
    }

    [Fact]
    public void SetDescription_WithTheSameText_ChangesNothing()
    {
        var item = NewItem("Same");
        var version = item.Version;

        item.SetDescription("Same  ", Guid.NewGuid(), DateTimeOffset.UtcNow);

        item.Version.Should().Be(version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n ")]
    [InlineData(null)]
    public void SetDescription_WithBlankText_ClearsTheDescription(string? blank)
    {
        var item = NewItem("Something");

        item.SetDescription(blank, Guid.NewGuid(), DateTimeOffset.UtcNow);

        item.DescriptionMd.Should().BeNull();
    }

    [Fact]
    public void SetDescription_OverTheLimit_IsRejected_AndLeavesTheItemUnchanged()
    {
        var item = NewItem("Keep");
        var version = item.Version;

        var act = () => item.SetDescription(new string('x', BoardItem.DescriptionMaxLength + 1), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Item_DescriptionTooLong);
        item.DescriptionMd.Should().Be("Keep");
        item.Version.Should().Be(version);
    }

    [Fact]
    public void SetDescription_OnAnArchivedItem_IsRejected()
    {
        var item = NewItem("Keep");
        item.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => item.SetDescription("New", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
        item.DescriptionMd.Should().Be("Keep");
    }
}
