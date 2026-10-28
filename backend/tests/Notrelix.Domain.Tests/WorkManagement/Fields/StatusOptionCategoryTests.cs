using FluentAssertions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.Tests.WorkManagement.Fields;

/// <summary>
/// Status options carry a work-status category (todo, in progress, review, done).
/// The category is required for options of a Status field, absent for every other
/// option-backed field, editable, and a Status field never loses its last Todo or
/// its last Done option.
/// </summary>
public class StatusOptionCategoryTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 3, 40, 0, TimeSpan.Zero);

    private static BoardField StatusField() =>
        BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a1"), Actor, Now);

    private static BoardField SelectField() =>
        BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Priority", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a2"), Actor, Now);

    private static BoardField StatusFieldWith(params (string Name, WorkStatusCategory Category)[] options)
    {
        var field = StatusField();
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, options.Length);
        for (var i = 0; i < options.Length; i++)
            field.AddOption(options[i].Name, Color.Create("#808080"), keys[i], Actor, Now, options[i].Category);
        ((IHasDomainEvents)field).ClearDomainEvents();
        return field;
    }

    [Fact]
    public void AddOption_ToStatusField_WithoutCategory_IsRejected_AndLeavesFieldUnchanged()
    {
        var field = StatusField();
        var version = field.Version;

        var act = () => field.AddOption("Cần làm", Color.Create("#808080"), FractionalIndex.Create("a0"), Actor, Now);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_StatusOptionCategoryRequired);
        field.Options.Should().BeEmpty();
        field.Version.Should().Be(version);
    }

    [Fact]
    public void AddOption_ToStatusField_StoresTheCategory()
    {
        var field = StatusField();

        var option = field.AddOption("Chờ duyệt", Color.Create("#808080"), FractionalIndex.Create("a0"), Actor, Now, WorkStatusCategory.Review);

        option.Category.Should().Be(WorkStatusCategory.Review);
    }

    [Fact]
    public void AddOption_ToSelectField_WithCategory_IsRejected()
    {
        var field = SelectField();

        var act = () => field.AddOption("High", Color.Create("#808080"), FractionalIndex.Create("a0"), Actor, Now, WorkStatusCategory.Done);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_OptionCategoryNotAllowed);
        field.Options.Should().BeEmpty();
    }

    [Fact]
    public void AddOption_ToSelectField_HasNoCategory()
    {
        var field = SelectField();

        var option = field.AddOption("High", Color.Create("#808080"), FractionalIndex.Create("a0"), Actor, Now);

        option.Category.Should().BeNull();
    }

    [Fact]
    public void ChangingTheCategory_BumpsTheFieldVersion_AndKeepsNameAndColor()
    {
        var field = StatusFieldWith(("Todo", WorkStatusCategory.Todo), ("Doing", WorkStatusCategory.InProgress), ("Done", WorkStatusCategory.Done));
        var doing = field.Options.Single(o => o.Name == "Doing");
        var version = field.Version;

        field.UpdateOption(doing.Id, "Doing", Color.Create("#808080"), Actor, Now.AddMinutes(1), WorkStatusCategory.Review);

        doing.Category.Should().Be(WorkStatusCategory.Review);
        field.Version.Should().Be(version + 1);
        field.DomainEvents.Should().ContainSingle(e => e is FieldOptionUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateOption_WithoutCategory_KeepsTheCurrentCategory()
    {
        var field = StatusFieldWith(("Todo", WorkStatusCategory.Todo), ("Doing", WorkStatusCategory.InProgress), ("Done", WorkStatusCategory.Done));
        var doing = field.Options.Single(o => o.Name == "Doing");

        field.UpdateOption(doing.Id, "Working", Color.Create("#808080"), Actor, Now.AddMinutes(1));

        doing.Name.Should().Be("Working");
        doing.Category.Should().Be(WorkStatusCategory.InProgress);
    }

    [Fact]
    public void UpdateOption_WithTheSameCategory_IsASemanticNoOp()
    {
        var field = StatusFieldWith(("Todo", WorkStatusCategory.Todo), ("Done", WorkStatusCategory.Done));
        var done = field.Options.Single(o => o.Name == "Done");
        var version = field.Version;

        field.UpdateOption(done.Id, "Done", Color.Create("#808080"), Actor, Now.AddMinutes(1), WorkStatusCategory.Done);

        field.Version.Should().Be(version);
        field.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateOption_OfSelectField_WithCategory_IsRejected()
    {
        var field = SelectField();
        var option = field.AddOption("High", Color.Create("#808080"), FractionalIndex.Create("a0"), Actor, Now);

        var act = () => field.UpdateOption(option.Id, "High", Color.Create("#808080"), Actor, Now, WorkStatusCategory.Done);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_OptionCategoryNotAllowed);
        option.Category.Should().BeNull();
    }

    [Theory]
    [InlineData(WorkStatusCategory.Todo)]
    [InlineData(WorkStatusCategory.Done)]
    public void RecategorizingTheLastTodoOrDone_IsRejected_AndChangesNothing(WorkStatusCategory last)
    {
        var field = StatusFieldWith(("Todo", WorkStatusCategory.Todo), ("Doing", WorkStatusCategory.InProgress), ("Done", WorkStatusCategory.Done));
        var option = field.Options.Single(o => o.Category == last);
        var version = field.Version;

        var act = () => field.UpdateOption(option.Id, "Renamed", Color.Create("#000000"), Actor, Now, WorkStatusCategory.InProgress);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_StatusRequiresTodoAndDone);
        option.Category.Should().Be(last);
        option.Name.Should().NotBe("Renamed");
        field.Version.Should().Be(version);
    }

    [Theory]
    [InlineData(WorkStatusCategory.Todo)]
    [InlineData(WorkStatusCategory.Done)]
    public void RemovingTheLastTodoOrDone_IsRejected(WorkStatusCategory last)
    {
        var field = StatusFieldWith(("Todo", WorkStatusCategory.Todo), ("Doing", WorkStatusCategory.InProgress), ("Done", WorkStatusCategory.Done));
        var option = field.Options.Single(o => o.Category == last);

        var act = () => field.RemoveOption(option.Id, Actor, Now);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_StatusRequiresTodoAndDone);
        field.Options.Should().HaveCount(3);
    }

    [Fact]
    public void RemovingATodo_WhenAnotherTodoRemains_IsAllowed()
    {
        var field = StatusFieldWith(("Backlog", WorkStatusCategory.Todo), ("Todo", WorkStatusCategory.Todo), ("Done", WorkStatusCategory.Done));
        var backlog = field.Options.Single(o => o.Name == "Backlog");

        field.RemoveOption(backlog.Id, Actor, Now);

        field.Options.Select(o => o.Name).Should().BeEquivalentTo(["Todo", "Done"]);
    }

    [Fact]
    public void RemovingAnInProgressOption_IsAllowed()
    {
        var field = StatusFieldWith(("Todo", WorkStatusCategory.Todo), ("Doing", WorkStatusCategory.InProgress), ("Done", WorkStatusCategory.Done));

        field.RemoveOption(field.Options.Single(o => o.Name == "Doing").Id, Actor, Now);

        field.Options.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("vi")]
    [InlineData("fr")]
    [InlineData(null)]
    public void DefaultStatuses_FollowTheCreatorLocale_KeepingColoursAndCategories(string? locale)
    {
        var field = StatusField();

        field.AddDefaultStatusOptions(Actor, Now, locale);

        var english = locale is "en" or "en-US";
        var o = field.Options.OrderBy(x => x.Position).ToList();
        o.Select(x => x.Name).Should().Equal(english
            ? new[] { "To do", "In progress", "In review", "Done" }
            : new[] { "Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành" });
        o.Select(x => x.Category).Should().Equal(WorkStatusCategory.Todo, WorkStatusCategory.InProgress, WorkStatusCategory.Review, WorkStatusCategory.Done);
        o.Select(x => x.Color.Value).Should().Equal("#7A8391", "#1E7BE0", "#C18A12", "#2E8B57");
    }

    [Fact]
    public void DefaultStatuses_AreSeededInOrder_WithTheirCategories()
    {
        var field = StatusField();

        field.AddDefaultStatusOptions(Actor, Now);

        field.Options.OrderBy(o => o.Position).Select(o => (o.Name, o.Category)).Should().Equal(
            ("Cần làm", WorkStatusCategory.Todo),
            ("Đang làm", WorkStatusCategory.InProgress),
            ("Chờ duyệt", WorkStatusCategory.Review),
            ("Hoàn thành", WorkStatusCategory.Done));
        field.Options.Select(o => o.Color.Value).Should().OnlyHaveUniqueItems();
        field.Options.Select(o => o.Position.Value).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void DefaultStatuses_RequireAnEmptyStatusField()
    {
        var seeded = StatusField();
        seeded.AddDefaultStatusOptions(Actor, Now);
        var select = SelectField();

        var again = () => seeded.AddDefaultStatusOptions(Actor, Now);
        var onSelect = () => select.AddDefaultStatusOptions(Actor, Now);

        again.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_DefaultStatusesRequireEmptyStatusField);
        onSelect.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_DefaultStatusesRequireEmptyStatusField);
        seeded.Options.Should().HaveCount(4);
        select.Options.Should().BeEmpty();
    }
}
