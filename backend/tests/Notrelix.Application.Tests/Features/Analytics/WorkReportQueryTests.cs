using Notrelix.Application.Features.Analytics.Reports.Queries.GetBoardReport;
using Notrelix.Application.Features.Analytics.Reports.Queries.GetWorkspaceReport;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.Reports;
using Notrelix.Application.Features.Workspaces.Public.SpaceSubtree;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Analytics;

public class WorkReportQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid BoardId = Guid.CreateVersion7();
    private static readonly Guid OtherBoardId = Guid.CreateVersion7();
    private static readonly Guid SpaceId = Guid.CreateVersion7();

    private readonly Mock<IWorkReportFacts> _facts = new();
    private readonly Mock<IResourceVisibility> _visibility = new();
    private readonly Mock<ISpaceSubtreeFacts> _spaces = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public WorkReportQueryTests()
    {
        _context.Setup(c => c.UserId).Returns(UserId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(WorkspaceId);
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _visibility.Setup(v => v.VisibleBoardIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BoardId, OtherBoardId]);
        _visibility.Setup(v => v.VisibleSpaceIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([SpaceId]);
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                (IReadOnlyList<ActorSnapshot>)ids.Select(id => new ActorSnapshot(id, "Person", null)).ToList());
        _facts.Setup(f => f.ReportAsync(
                WorkspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), Now, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkReportFact(
                3, 2, 1, 1,
                [new WorkReportStatusFact(null, null, null, "Todo", 2)],
                [new WorkReportAssigneeFact(UserId, 3, 2, 1), new WorkReportAssigneeFact(null, 0, 0, 0)],
                [new WorkReportWeekFact(Now, 1)],
                3600));
    }

    [Fact]
    public async Task A_board_report_names_the_assignees_and_covers_only_that_board()
    {
        var handler = new GetBoardReportQueryHandler(
            _facts.Object, _visibility.Object, _actors.Object, _context.Object, _clock.Object);

        var result = await handler.Handle(new GetBoardReportQuery(BoardId), CancellationToken.None);

        Assert.Equal(BoardId, result.Data!.BoardId);
        Assert.Equal(3, result.Data.Report.Total);
        Assert.Equal("Person", result.Data.Report.ByAssignee[0].Name);
        Assert.Null(result.Data.Report.ByAssignee[1].Name);
        _facts.Verify(f => f.ReportAsync(
            WorkspaceId, It.Is<IReadOnlyCollection<Guid>>(b => b.Single() == BoardId), Now, 8, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task A_board_the_caller_cannot_read_is_not_found()
    {
        var handler = new GetBoardReportQueryHandler(
            _facts.Object, _visibility.Object, _actors.Object, _context.Object, _clock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetBoardReportQuery(Guid.CreateVersion7()), CancellationToken.None));
        _facts.Verify(f => f.ReportAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private GetWorkspaceReportQueryHandler WorkspaceHandler() =>
        new(_facts.Object, _visibility.Object, _spaces.Object, _actors.Object, _context.Object, _clock.Object);

    [Fact]
    public async Task A_workspace_report_covers_exactly_the_readable_boards()
    {
        var result = await WorkspaceHandler().Handle(new GetWorkspaceReportQuery(WorkspaceId), CancellationToken.None);

        Assert.Equal(2, result.Data!.BoardCount);
        Assert.Null(result.Data.SpaceId);
        _facts.Verify(f => f.ReportAsync(
            WorkspaceId, It.Is<IReadOnlyCollection<Guid>>(b => b.Count == 2), Now, 8, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Narrowing_to_a_department_keeps_only_its_boards_among_the_readable_ones()
    {
        _spaces.Setup(s => s.GetSubtreeAsync(WorkspaceId, SpaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([SpaceId]);
        _facts.Setup(f => f.BoardIdsInSpacesAsync(
                WorkspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([BoardId]);

        var result = await WorkspaceHandler().Handle(
            new GetWorkspaceReportQuery(WorkspaceId, SpaceId), CancellationToken.None);

        Assert.Equal(1, result.Data!.BoardCount);
        Assert.Equal(SpaceId, result.Data.SpaceId);
    }

    [Fact]
    public async Task A_space_the_caller_cannot_see_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => WorkspaceHandler().Handle(
            new GetWorkspaceReportQuery(WorkspaceId, Guid.CreateVersion7()), CancellationToken.None));
    }

    [Fact]
    public void Filtering_by_space_needs_the_space_management_permission()
    {
        Assert.Equal(PermissionAction.ViewWorkspace, new GetWorkspaceReportQuery(WorkspaceId).Action);
        Assert.Equal(PermissionAction.ManageSpaces, new GetWorkspaceReportQuery(WorkspaceId, SpaceId).Action);
    }
}
