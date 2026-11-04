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
}
