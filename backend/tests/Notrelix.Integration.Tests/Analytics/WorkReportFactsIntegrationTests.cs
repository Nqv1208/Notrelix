using Notrelix.Application.Features.WorkManagement.Public.Reports;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Analytics;

/// <summary>
/// The published work report measures against a real PostgreSQL: every figure is one GROUP BY over the
/// Boards' Items, scoped to the Boards the caller passes. A Board the caller did not pass never contributes.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkReportFactsIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 10, 0, 0, TimeSpan.Zero); // a Wednesday

    private readonly PostgresTestContainer _db;
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _assignee = Guid.NewGuid();
    private Workspace _workspace = null!;
    private Board _board = null!;
    private Board _other = null!;
    private BoardField _status = null!;

    public WorkReportFactsIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

        _workspace = Workspace.Create(_accountId, _userId, "Report WS", $"rep-{Guid.NewGuid():N}"[..16], Now.AddDays(-30));
        _board = Board.Create(_accountId, _workspace.Id, _userId, "Report board", null, Now.AddDays(-30));
        _other = Board.Create(_accountId, _workspace.Id, _userId, "Other board", null, Now.AddDays(-30));
        var group = Group(_board);
        var otherGroup = Group(_other);

        _status = BoardField.Create(
            _accountId, _workspace.Id, _board.Id, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":[]}")),
            FractionalIndex.Create("a0"), _userId, Now.AddDays(-30), isSystem: true);
        _status.AddDefaultStatusOptions(_userId, Now.AddDays(-30));
        var todo = _status.Options.First(o => o.Category == WorkStatusCategory.Todo);
        var done = _status.Options.First(o => o.Category == WorkStatusCategory.Done);

        // Open, overdue, in Todo, assigned.
        var open = Item(_board, group, "open overdue", "a0", Now.AddDays(-10), due: Now.AddDays(-2));
        open.UpdateFieldValue(_status, OptionValue(todo.Id), _userId, Now.AddDays(-9));
        // Done 3 days ago after 7 days of work (this week's Monday is 2 days back, so 3 days ago is last week).
        var finished = Item(_board, group, "finished", "a1", Now.AddDays(-10));
        finished.UpdateFieldValue(_status, OptionValue(done.Id), _userId, Now.AddDays(-3));
        finished.Complete(Now.AddDays(-3), _userId, Now.AddDays(-3));
        // Done yesterday, no status, unassigned, after 3 days.
        var recent = Item(_board, group, "recent", "a2", Now.AddDays(-4));
        recent.Complete(Now.AddDays(-1), _userId, Now.AddDays(-1));
        // Deleted and archived items never count.
        var gone = Item(_board, group, "deleted", "a3", Now.AddDays(-5));
        gone.Delete(_userId, Now.AddDays(-4));
        var archived = Item(_board, group, "archived", "a4", Now.AddDays(-5));
        archived.Archive(_userId, Now.AddDays(-4));
        // An item of another board that the report must leave out.
        var foreign = Item(_other, otherGroup, "foreign", "a0", Now.AddDays(-5));

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var seed = _db.CreateOwnerContext(tenant);
        seed.Workspaces.Add(_workspace);
        seed.Boards.AddRange(_board, _other);
        seed.BoardGroups.AddRange(group, otherGroup);
        seed.BoardFields.Add(_status);
        seed.BoardItems.AddRange(open, finished, recent, gone, archived, foreign);
        seed.BoardItemMembers.Add(BoardItemMember.Create(
            _accountId, _workspace.Id, _board.Id, open.Id, _assignee, _userId, Now.AddDays(-9)));
        seed.BoardItemMembers.Add(BoardItemMember.Create(
            _accountId, _workspace.Id, _board.Id, finished.Id, _assignee, _userId, Now.AddDays(-9)));
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private BoardGroup Group(Board board) =>
        BoardGroup.Create(_accountId, _workspace.Id, board.Id, "G", Color.Create("#808080"),
            FractionalIndex.Create("a0"), _userId, Now.AddDays(-30));

    private BoardItem Item(Board board, BoardGroup group, string name, string position, DateTimeOffset createdAt,
        DateTimeOffset? due = null) =>
        BoardItem.CreateRoot(_accountId, _workspace.Id, board.Id, group.Id, name, FractionalIndex.Create(position),
            _userId, createdAt, dueAt: due);

    private static FieldValue OptionValue(Guid optionId) =>
        FieldValue.Create(JsonValue.Create($"\"{optionId}\""));

    private async Task<WorkReportFact> ReportAsync(params Guid[] boards)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_accountId, _workspace.Id, _userId);
        await using var context = _db.CreateOwnerContext(tenant);
        return await new WorkReportFactsService(context).ReportAsync(_workspace.Id, boards, Now, 8, CancellationToken.None);
    }

    [Fact]
    public async Task A_board_report_counts_live_items_by_status_assignee_week_and_completion_time()
    {
        var report = await ReportAsync(_board.Id);

        report.Total.Should().Be(3, "the deleted, the archived and the other board's items do not count");
        report.Open.Should().Be(1);
        report.Done.Should().Be(2);
        report.Overdue.Should().Be(1, "only the open item past its due time");

        report.ByStatus.Select(s => (s.Name, s.Category, s.Count)).Should().Contain(
        [
            ("Cần làm", "Todo", 1),
            ("Hoàn thành", "Done", 1),
            ("Đang làm", "InProgress", 0),
        ]);
        report.ByStatus.Last().Should().Be(new WorkReportStatusFact(null, null, null, "None", 1));

        report.ByAssignee.Single(a => a.UserId == _assignee).Should().Be(new WorkReportAssigneeFact(_assignee, 2, 1, 1));
        report.ByAssignee.Single(a => a.UserId == null).Should().Be(new WorkReportAssigneeFact(null, 1, 0, 0));

        report.CompletedPerWeek.Should().HaveCount(8);
        report.CompletedPerWeek.Last().WeekStart.Should().Be(new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.Zero), "Monday of the current week");
        report.CompletedPerWeek.Last().Completed.Should().Be(1, "completed yesterday");
        report.CompletedPerWeek[^2].Completed.Should().Be(1, "completed three days ago, in the previous week");
        report.CompletedPerWeek.Sum(w => w.Completed).Should().Be(2);

        // 7 days and 3 days: 5 days on average.
        report.AverageCompletionSeconds.Should().BeApproximately(TimeSpan.FromDays(5).TotalSeconds, 1);
    }

    [Fact]
    public async Task A_report_over_several_boards_merges_status_by_category_and_never_adds_unlisted_boards()
    {
        var both = await ReportAsync(_board.Id, _other.Id);
        both.Total.Should().Be(4);
        both.ByStatus.Take(4).Select(s => s.Category).Should().Equal("Todo", "InProgress", "Review", "Done");
        both.ByStatus.Take(4).Should().OnlyContain(s => s.OptionId == null && s.Name == null);
        both.ByStatus.Single(s => s.Category == "None").Count.Should().Be(2);

        var none = await ReportAsync();
        none.Total.Should().Be(0);
        none.CompletedPerWeek.Should().HaveCount(8);
        none.AverageCompletionSeconds.Should().BeNull();
    }

    [Fact]
    public async Task Boards_are_found_by_the_spaces_they_are_placed_in_but_only_among_those_passed()
    {
        var spaceId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seed = _db.CreateOwnerContext(tenant))
        {
            var placed = await seed.Boards.FindAsync(_board.Id);
            placed!.PlaceInSpace(spaceId, _userId, Now);
            await seed.SaveChangesAsync();
        }

        var scope = new FakeCurrentTenantContext();
        scope.SetWorkspace(_accountId, _workspace.Id, _userId);
        await using var context = _db.CreateOwnerContext(scope);
        var adapter = new WorkReportFactsService(context);

        (await adapter.BoardIdsInSpacesAsync(_workspace.Id, [_board.Id, _other.Id], [spaceId], CancellationToken.None))
            .Should().Equal(_board.Id);
        (await adapter.BoardIdsInSpacesAsync(_workspace.Id, [_other.Id], [spaceId], CancellationToken.None))
            .Should().BeEmpty("a board the caller did not pass is never added");
    }
}
