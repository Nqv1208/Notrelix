using FluentAssertions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Domain.Tests.WorkManagement;

public class KanbanWipLimitsTests
{
    private static readonly string Todo = Guid.NewGuid().ToString();
    private static readonly string Doing = Guid.NewGuid().ToString();

    private static BoardViewConfig Config(string json) => BoardViewConfig.Create(JsonValue.Create(json));

    private static string LimitJson(string limit) => "{\"wipLimits\":{\"" + Todo + "\":" + limit + "}}";

    [Fact]
    public void ValidMap_IsKeptPerLane()
    {
        var config = Config("{\"laneCardLimit\":5,\"wipLimits\":{\"" + Todo + "\":1,\"" + Doing + "\":999}}");

        var limits = KanbanWipLimits.FromConfig(config.Data);

        limits.LimitFor(Todo).Should().Be(1);
        limits.LimitFor(Doing).Should().Be(999);
        limits.LimitFor("lane-without-limit").Should().BeNull();
        config.Data.Value.Should().Contain("\"laneCardLimit\":5", "other presentation keys are carried as given");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("1000")]
    public void LimitOutOfRange_IsRejected(string limit)
    {
        var act = () => Config(LimitJson(limit));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Kanban_WipLimitOutOfRange);
    }

    [Theory]
    [InlineData("2.5")]
    [InlineData("\"3\"")]
    [InlineData("true")]
    public void LimitThatIsNotAWholeNumber_IsRejected(string limit)
    {
        var act = () => Config(LimitJson(limit));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Kanban_WipLimitsInvalid);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("5")]
    [InlineData("\"x\"")]
    public void LimitsThatAreNotAMap_AreRejected(string value)
    {
        var act = () => Config("{\"wipLimits\":" + value + "}");

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Kanban_WipLimitsInvalid);
    }

    [Fact]
    public void BlankLaneId_IsRejected()
    {
        var act = () => Config("""{"wipLimits":{" ":3}}""");

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Kanban_WipLimitsInvalid);
    }

    [Theory]
    [InlineData("""{"wipLimits":{}}""")]
    [InlineData("""{"wipLimits":null}""")]
    [InlineData("""{"laneCardLimit":3}""")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void EmptyOrAbsent_MeansNoLimits(string json)
    {
        var limits = KanbanWipLimits.FromConfig(Config(json).Data);

        limits.Should().Be(KanbanWipLimits.None);
        limits.Limits.Should().BeEmpty();
    }

    [Fact]
    public void LaneIdThatNamesNoLane_IsKept_AndIgnoredByTheView()
    {
        var limits = KanbanWipLimits.FromConfig(Config("""{"wipLimits":{"deleted-option":4}}""").Data);

        limits.LimitFor("deleted-option").Should().Be(4);
    }

    [Fact]
    public void Create_RejectsOutOfRange_AndEqualityIgnoresOrder()
    {
        var act = () => KanbanWipLimits.Create([new(Todo, 0)]);
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Kanban_WipLimitOutOfRange);

        KanbanWipLimits.Create([new(Todo, 2), new(Doing, 3)])
            .Should().Be(KanbanWipLimits.Create([new(Doing, 3), new(Todo, 2)]));
    }

    [Fact]
    public void KanbanView_AcceptsPresentationConfigUpdate_WithLimits()
    {
        var view = BoardView.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Kanban", ViewType.Kanban,
            BoardViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var versionBefore = view.Version;

        view.UpdateConfig(Config(LimitJson("3")), Guid.NewGuid(), DateTimeOffset.UtcNow);

        KanbanWipLimits.FromConfig(view.Config.Data).LimitFor(Todo).Should().Be(3);
        view.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void TableView_StillRejectsATypedConfigOfAnotherType()
    {
        var view = BoardView.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Table", ViewType.Table,
            TableViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => view.UpdateConfig(CalendarViewConfig.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_View_TableMustUseTableConfig);
    }
}
