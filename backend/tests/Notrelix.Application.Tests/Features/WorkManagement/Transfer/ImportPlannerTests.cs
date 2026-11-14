using Notrelix.Application.Features.WorkManagement.Transfer;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.Transfer;

/// <summary>What a file would do to a board: the mapping check, the row errors and the planned items (nothing is written).</summary>
public class ImportPlannerTests : WorkManagementHandlerTestBase
{
    private readonly Guid _boardId = Guid.CreateVersion7();
    private readonly BoardField _title;
    private readonly BoardField _number;
    private readonly BoardField _assignee;
    private readonly BoardGroup _group;
    private readonly ImportSchema _schema;

    public ImportPlannerTests()
    {
        _title = BoardField.Create(TestAccountId, TestWorkspaceId, _boardId, "Title", FieldType.Text,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), TestUserId, TestNow, isSystem: true);
        _number = BoardField.Create(TestAccountId, TestWorkspaceId, _boardId, "Budget", FieldType.Number,
            FieldSettings.Empty(), FractionalIndex.Create("a1"), TestUserId, TestNow);
        _assignee = BoardField.Create(TestAccountId, TestWorkspaceId, _boardId, "Assignee", FieldType.Person,
            FieldSettings.Empty(), FractionalIndex.Create("a2"), TestUserId, TestNow);
        _group = BoardGroup.Create(TestAccountId, TestWorkspaceId, _boardId, "To do", Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        _schema = new ImportSchema([_title, _number, _assignee], [_group]);
    }

    private static TabularData File(params string?[][] rows) =>
        new("csv", ["Name", "Budget"], rows.Select((cells, i) => new TabularRow(i + 1, cells)).ToList(), rows.Length);

    [Fact]
    public void Targets_LockAPersonField_AndSuggestMatchesByNameOrKnownWords()
    {
        var targets = ImportPlanner.Targets(_schema);

        targets.Single(t => t.Id == _assignee.Id.ToString()).LockedReason.Should().Be(ImportPlanner.LockedPerson);
        var suggested = ImportPlanner.Suggest(["Name", "Budget", "Assignee"], targets);
        suggested.Select(m => m.Target).Should().Equal(ImportPlanner.Title, _number.Id.ToString());
    }

    [Fact]
    public void Evaluate_ANumberCellThatIsNotANumber_FlagsTheColumnAndNamesTheRow()
    {
        var map = new[] { new ColumnMap(0, ImportPlanner.Title), new ColumnMap(1, _number.Id.ToString()) };

        var result = ImportPlanner.Evaluate(File(["One", "1.234,5"], ["Two", "ba ngày"]), _schema, map, collectItems: true);

        result.Checks.Single(c => c.Target == _number.Id.ToString()).Status.Should().Be("incompatible");
        result.Errors.Should().ContainSingle(e => e.Row == 2 && e.Code == "not-number" && e.Value == "ba ngày");
        result.ErrorRows.Should().Be(1);
        result.Items.Select(i => i.Row).Should().Equal(1);
    }

    [Fact]
    public void Evaluate_AMissingTitle_FailsTheRow_AndALockedColumnIsNotImported()
    {
        var map = new[] { new ColumnMap(0, ImportPlanner.Title), new ColumnMap(1, _assignee.Id.ToString()) };

        var result = ImportPlanner.Evaluate(File(["", "someone@example.com"], ["Ok", "x"]), _schema, map, collectItems: true);

        result.Errors.Should().ContainSingle(e => e.Row == 1 && e.Code == "required");
        result.Checks.Should().ContainSingle().Which.Target.Should().Be(ImportPlanner.Title);
        result.Items.Should().ContainSingle().Which.Values.Should().BeEmpty();
    }

    [Theory]
    [InlineData("2,5", 2.5)]
    [InlineData("1,234.5", 1234.5)]
    [InlineData("1.234,5", 1234.5)]
    public void TryNumber_ReadsVietnameseAndEnglishNumbers(string raw, double expected)
    {
        ImportPlanner.TryNumber(raw, out var value).Should().BeTrue();
        value.Should().Be((decimal)expected);
    }

    private static readonly Guid Ana = Guid.CreateVersion7();
    private static readonly Guid Binh = Guid.CreateVersion7();

    private ImportSchema SchemaWithPeople() => new([_title, _number, _assignee], [_group],
        new Dictionary<string, Guid> { ["ana@example.com"] = Ana, ["binh@example.com"] = Binh });

    [Fact]
    public void Targets_APersonColumnIsImportable_OnlyWhenTheImporterMayMatchPeople()
    {
        ImportPlanner.Targets(SchemaWithPeople()).Single(t => t.Id == _assignee.Id.ToString()).LockedReason.Should().BeNull();
        ImportPlanner.Targets(_schema).Single(t => t.Id == _assignee.Id.ToString()).LockedReason.Should().Be(ImportPlanner.LockedPerson);
    }

    [Fact]
    public void Evaluate_AnEmailOfAnOfferedPerson_AssignsThemCaseInsensitively()
    {
        var map = new[] { new ColumnMap(0, ImportPlanner.Title), new ColumnMap(1, _assignee.Id.ToString()) };

        var result = ImportPlanner.Evaluate(File(["One", " Ana@Example.com "]), SchemaWithPeople(), map, collectItems: true);

        result.ErrorCount.Should().Be(0);
        result.UnmatchedPersonRows.Should().BeEmpty();
        result.Items.Single().Values.Single().Value.Data.Value.Should().Be($"\"{Ana}\"");
    }

    [Fact]
    public void Evaluate_AnUnmatchedEmail_LeavesTheCellEmptyAndListsTheRow_WithoutFailingTheFile()
    {
        var map = new[] { new ColumnMap(0, ImportPlanner.Title), new ColumnMap(1, _assignee.Id.ToString()) };

        var result = ImportPlanner.Evaluate(
            File(["One", "ana@example.com"], ["Two", "stranger@elsewhere.com"], ["Three", "stranger@elsewhere.com"], ["Four", ""]),
            SchemaWithPeople(), map, collectItems: true);

        result.ErrorCount.Should().Be(0);
        result.UnmatchedPersonRows.Should().Equal(2, 3);
        result.Items.Should().HaveCount(4);
        result.Items.Single(i => i.Row == 2).Values.Should().BeEmpty();
        result.Items.Single(i => i.Row == 4).Values.Should().BeEmpty();
        result.Checks.Single(c => c.Target == _assignee.Id.ToString()).Status.Should().Be("ok");
    }

    [Fact]
    public void Evaluate_APersonColumn_IsIgnoredWhenTheImporterMayNotMatchPeople()
    {
        var map = new[] { new ColumnMap(0, ImportPlanner.Title), new ColumnMap(1, _assignee.Id.ToString()) };

        var result = ImportPlanner.Evaluate(File(["One", "ana@example.com"]), _schema, map, collectItems: true);

        result.UnmatchedPersonRows.Should().BeEmpty();
        result.Items.Single().Values.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_AMultiplePersonField_TakesSeveralEmails_AndListsTheRowWhenOneIsUnknown()
    {
        var many = BoardField.Create(TestAccountId, TestWorkspaceId, _boardId, "Reviewers", FieldType.Person,
            FieldSettings.Create(Notrelix.Domain.SharedKernel.JsonValue.Create("{\"multiple\":true}")), FractionalIndex.Create("a3"), TestUserId, TestNow);
        var schema = new ImportSchema([_title, many], [_group], new Dictionary<string, Guid> { ["ana@example.com"] = Ana, ["binh@example.com"] = Binh });
        var map = new[] { new ColumnMap(0, ImportPlanner.Title), new ColumnMap(1, many.Id.ToString()) };

        var result = ImportPlanner.Evaluate(File(["One", "ana@example.com; binh@example.com; nobody@x.com"]), schema, map, collectItems: true);

        result.UnmatchedPersonRows.Should().Equal(1);
        result.Items.Single().Values.Single().Value.Data.Value.Should().Be($"[\"{Ana}\",\"{Binh}\"]");
    }

    [Fact]
    public void Evaluate_ARequiredFieldTheRowLeavesEmpty_TakesItsDefault_LikeAnyCreate()
    {
        var code = RequiredText("Code", "\"N/A\"");
        var schema = new ImportSchema([_title, code], [_group]);
        var map = new[] { new ColumnMap(0, ImportPlanner.Title), new ColumnMap(1, code.Id.ToString()) };

        var result = ImportPlanner.Evaluate(File(["One", ""], ["Two", "X-2"]), schema, map, collectItems: true);

        result.ErrorCount.Should().Be(0);
        result.Items.Select(i => i.Values.Single().Value.Data.Value).Should().Equal("\"N/A\"", "\"X-2\"");
        ImportPlanner.Targets(schema).Single(t => t.Id == code.Id.ToString()).Required.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ARequiredFieldWithNoValueAndNoDefault_FailsTheRowNamingTheField_MappedOrNot()
    {
        var code = RequiredText("Code", defaultJson: null);
        var schema = new ImportSchema([_title, code], [_group]);
        var mapped = new[] { new ColumnMap(0, ImportPlanner.Title), new ColumnMap(1, code.Id.ToString()) };
        var unmapped = new[] { new ColumnMap(0, ImportPlanner.Title) };

        var withColumn = ImportPlanner.Evaluate(File(["One", ""], ["Two", "X-2"]), schema, mapped, collectItems: true);
        var withoutColumn = ImportPlanner.Evaluate(File(["One", "ignored"]), schema, unmapped, collectItems: true);

        withColumn.Errors.Should().ContainSingle(e => e.Row == 1 && e.Code == "required" && e.Target == code.Id.ToString() && e.Column == "Budget");
        withColumn.Items.Select(i => i.Row).Should().Equal(2);
        withoutColumn.Errors.Should().ContainSingle(e => e.Row == 1 && e.Code == "required" && e.Column == "Code");
        withoutColumn.Items.Should().BeEmpty("a file that does not check clean imports nothing");
    }

    /// <summary>
    /// A required field as an old board may hold it: the settings PATCH now refuses required without a default,
    /// but a field made required before that rule, or with a default given later, is what the import must meet.
    /// </summary>
    private BoardField RequiredText(string name, string? defaultJson)
    {
        var field = BoardField.Create(TestAccountId, TestWorkspaceId, _boardId, name, FieldType.Text,
            FieldSettings.Create(JsonValue.Create("{\"required\":true}")!), FractionalIndex.Create("a5"), TestUserId, TestNow);
        if (defaultJson is not null) field.SetDefaultValue(FieldValue.Create(JsonValue.Create(defaultJson)!), TestUserId, TestNow);
        return field;
    }
}
