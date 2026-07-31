using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.Tests.WorkManagement;

[CoversAggregate(typeof(BoardField))]
public class BoardFieldDefaultValueTests
{
    private static BoardField CreateTextField(FieldSettings settings)
    {
        return BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Notes", FieldType.Text, settings,
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }

    private static BoardField CreateSelectFieldWithOptions(out Guid optionId)
    {
        var field = BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Status", FieldType.Select, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        optionId = field.Options.First().Id;
        return field;
    }

    private static FieldValue Text(string value) => FieldValue.Create(JsonValue.Create($"\"{value}\""));

    [Fact]
    public void Create_WithNonStringTextDefault_ShouldReject()
    {
        var defaultValue = FieldValue.Create(JsonValue.Create("123"));

        Action act = () => BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Notes", FieldType.Text, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow,
            defaultValue);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidStringFormat);
    }

    [Fact]
    public void Create_WithNonNumberDefault_ShouldReject()
    {
        var defaultValue = FieldValue.Create(JsonValue.Create("\"high\""));

        Action act = () => BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Points", FieldType.Number, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow,
            defaultValue);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidStringValue);
    }

    [Fact]
    public void Create_WithInvalidDateDefault_ShouldReject()
    {
        var defaultValue = FieldValue.Create(JsonValue.Create("\"not-a-date\""));

        Action act = () => BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Due Date", FieldType.Date, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow,
            defaultValue);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidDateValue);
    }

    [Fact]
    public void Create_WithValidTextDefault_ShouldSucceed()
    {
        var defaultValue = Text("hello");

        var field = BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Notes", FieldType.Text, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow,
            defaultValue);

        field.DefaultValue.Should().Be(defaultValue);
    }

    [Fact]
    public void Create_WithOptionBackedDefault_ShouldReject()
    {
        var defaultValue = FieldValue.Create(JsonValue.Create($"\"{Guid.NewGuid()}\""));

        Action act = () => BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Status", FieldType.Select, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow,
            defaultValue);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_DefaultRequiresConfiguredOptions);
    }

    [CoversMutation(typeof(BoardField), "SetDefaultValue(Notrelix.Domain.WorkManagement.Fields.FieldValue,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SetDefaultValue_WithConfiguredOption_ShouldSucceed_AndRaiseEvent()
    {
        var field = CreateSelectFieldWithOptions(out var optionId);
        ((IHasDomainEvents)field).ClearDomainEvents();
        var defaultValue = FieldValue.Create(JsonValue.Create($"\"{optionId}\""));

        field.SetDefaultValue(defaultValue, Guid.NewGuid(), DateTimeOffset.UtcNow);

        field.DefaultValue.Should().Be(defaultValue);
        field.DomainEvents.Should().ContainSingle(e => e is BoardFieldDefaultValueUpdatedDomainEvent);
    }

    [CoversMutation(typeof(BoardField), "SetDefaultValue(Notrelix.Domain.WorkManagement.Fields.FieldValue,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SetDefaultValue_WithMultiSelectConfiguredOptions_ShouldSucceed()
    {
        var field = BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Tags", FieldType.MultiSelect, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("A", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("B", Color.Create("#FFFF00"), FractionalIndex.Create("a3"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var optionIds = field.Options.Select(o => o.Id).ToArray();
        var defaultValue = FieldValue.Create(JsonValue.Create($"[\"{optionIds[0]}\",\"{optionIds[1]}\"]"));

        field.SetDefaultValue(defaultValue, Guid.NewGuid(), DateTimeOffset.UtcNow);

        field.DefaultValue.Should().Be(defaultValue);
    }

    [CoversMutation(typeof(BoardField), "SetDefaultValue(Notrelix.Domain.WorkManagement.Fields.FieldValue,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void SetDefaultValue_WithUnconfiguredOption_ShouldReject()
    {
        var field = CreateSelectFieldWithOptions(out _);
        var defaultValue = FieldValue.Create(JsonValue.Create($"\"{Guid.NewGuid()}\""));

        Action act = () => field.SetDefaultValue(defaultValue, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_InvalidOptionValue);
    }

    [CoversMutation(typeof(BoardField), "SetDefaultValue(Notrelix.Domain.WorkManagement.Fields.FieldValue,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SetDefaultValue_WithNull_ShouldClear_AndRaiseEvent()
    {
        var field = CreateSelectFieldWithOptions(out var optionId);
        field.SetDefaultValue(FieldValue.Create(JsonValue.Create($"\"{optionId}\"")), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)field).ClearDomainEvents();

        field.SetDefaultValue(null, Guid.NewGuid(), DateTimeOffset.UtcNow);

        field.DefaultValue.Should().BeNull();
        field.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BoardFieldDefaultValueUpdatedDomainEvent>()
            .Which.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void UpdateSettings_ThatInvalidatesDefault_ShouldReject()
    {
        var field = CreateTextField(FieldSettings.Create(JsonValue.Create("{\"maxLength\":10}")));
        field.SetDefaultValue(Text("hello"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => field.UpdateSettings(
            FieldSettings.Create(JsonValue.Create("{\"maxLength\":3}")),
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldValue_TextExceedsMaxLength);
    }

    [Fact]
    public void RemoveOption_ReferencedByDefault_ShouldReject_AndKeepOptionAndDefault()
    {
        var field = CreateSelectFieldWithOptions(out var optionId);
        field.SetDefaultValue(FieldValue.Create(JsonValue.Create($"\"{optionId}\"")), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => field.RemoveOption(optionId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_OptionUsedByDefault);
        field.Options.Should().ContainSingle();
        field.DefaultValue.Should().NotBeNull();
    }

    [CoversMutation(typeof(BoardField), "SetDefaultValue(Notrelix.Domain.WorkManagement.Fields.FieldValue,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void SetDefaultValue_SameValueWithStaleTimestamp_ShouldNoOp()
    {
        var field = CreateSelectFieldWithOptions(out var optionId);
        var defaultValue = FieldValue.Create(JsonValue.Create($"\"{optionId}\""));
        var updatedBy = Guid.NewGuid();
        var updatedAt = DateTimeOffset.UtcNow;
        field.SetDefaultValue(defaultValue, updatedBy, updatedAt);
        ((IHasDomainEvents)field).ClearDomainEvents();

        Action act = () => field.SetDefaultValue(defaultValue, updatedBy, updatedAt.AddHours(-1));

        act.Should().NotThrow();
        field.DomainEvents.Should().BeEmpty();
        field.DefaultValue.Should().Be(defaultValue);
    }

    [Fact]
    public void RemoveOption_MissingOptionWithStaleTimestamp_ShouldReturnBusinessRuleBeforeAuditRule()
    {
        var field = CreateSelectFieldWithOptions(out _);
        var updatedAt = DateTimeOffset.UtcNow;
        field.AddOption("In Progress", Color.Create("#FFFF00"), FractionalIndex.Create("a3"), Guid.NewGuid(), updatedAt);

        Action act = () => field.RemoveOption(Guid.NewGuid(), Guid.NewGuid(), updatedAt.AddHours(-1));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_OptionNotFound);
    }

    [Fact]
    public void ReorderOptions_SameSequenceWithStaleTimestamp_ShouldNoOp()
    {
        var field = CreateSelectFieldWithOptions(out _);
        field.AddOption("In Progress", Color.Create("#FFFF00"), FractionalIndex.Create("a3"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var order = field.Options.OrderBy(o => o.Position).Select(o => o.Id).ToList();
        ((IHasDomainEvents)field).ClearDomainEvents();

        Action act = () => field.ReorderOptions(order, Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1));

        act.Should().NotThrow();
        field.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdatePosition_ShouldNotRaiseEvent()
    {
        var field = CreateSelectFieldWithOptions(out _);
        ((IHasDomainEvents)field).ClearDomainEvents();
        var newPosition = FractionalIndex.Create("a9");

        field.UpdatePosition(newPosition, Guid.NewGuid(), DateTimeOffset.UtcNow);

        field.Position.Should().Be(newPosition);
        field.DomainEvents.Should().BeEmpty();
    }
}
