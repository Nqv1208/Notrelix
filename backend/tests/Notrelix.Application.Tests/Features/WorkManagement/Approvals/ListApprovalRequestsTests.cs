using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Approvals.Queries.ListApprovalRequests;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.WorkManagement.Approvals;

/// <summary>UC-WM-B28: the approval requests aimed at one Board, newest first, with who was asked.</summary>
public class ListApprovalRequestsTests : WorkManagementHandlerTestBase
{
    private ApprovalRequest Request(Guid targetId, string kind, string title, int minutes, Guid? approver = null, bool deleted = false)
    {
        var at = TestNow.AddMinutes(minutes);
        var request = ApprovalRequest.Create(TestAccountId, TestWorkspaceId,
            ResourceRef.Create(ResourceKind.Create(kind), targetId, TestWorkspaceId), title, TestUserId, at);
        if (approver is { } id) request.AddStep(1, TestUserId, at, approverUserId: id);
        if (deleted) request.Delete(TestUserId, at);
        return request;
    }

    private Task<Result<List<ApprovalRequestDto>>> List(Guid boardId, TimeCursorPosition? after = null, int? limit = null)
    {
        return new ListApprovalRequestsQueryHandler(DbContextMock.Object)
            .Handle(new ListApprovalRequestsQuery(boardId, after, limit), CancellationToken.None);
    }

    [Fact]
    public async Task List_ReturnsOnlyThisBoardsLiveRequestsNewestFirstWithTheirSteps()
    {
        var boardId = Guid.CreateVersion7();
        var approver = Guid.CreateVersion7();
        var older = Request(boardId, "work-management.board", "Older", 0, approver);
        var newer = Request(boardId, "work-management.board", "Newer", 10);
        var removed = Request(boardId, "work-management.board", "Removed", 20, deleted: true);
        var otherBoard = Request(Guid.CreateVersion7(), "work-management.board", "Other board", 30);
        SetupApprovalRequests(older, newer, removed, otherBoard);
        SetupApprovalSteps(older.Steps.ToArray());

        var result = await List(boardId);

        result.Succeeded.Should().BeTrue();
        result.Data!.Select(r => r.Title).Should().Equal("Newer", "Older");
        result.Data.Single(r => r.Title == "Older").Steps.Should().ContainSingle(s => s.ApproverUserId == approver);
        result.Data.Single(r => r.Title == "Newer").Steps.Should().BeEmpty();
    }

    [Fact]
    public async Task List_WithAnEmptyBoard_IsAnEmptyList()
    {
        SetupApprovalRequests();

        var result = await List(Guid.CreateVersion7());

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task List_AfterACursorWithALimit_ReadsTheNextPage()
    {
        var boardId = Guid.CreateVersion7();
        var all = Enumerable.Range(0, 5).Select(i => Request(boardId, "work-management.board", $"R{i}", i)).ToArray();
        SetupApprovalRequests(all);

        var first = (await List(boardId, limit: 2)).Data!;
        var cursor = new TimeCursorPosition(first[^1].CreatedAt, first[^1].Id);
        var second = (await List(boardId, cursor, 2)).Data!;

        first.Select(r => r.Title).Should().Equal("R4", "R3");
        second.Select(r => r.Title).Should().Equal("R2", "R1");
    }

    [Fact]
    public void Query_RequiresViewBoardOnTheBoard_AndAnId()
    {
        var boardId = Guid.CreateVersion7();
        var query = new ListApprovalRequestsQuery(boardId);

        query.Action.Should().Be(PermissionAction.ViewBoard);
        query.Resource.ResourceId.Should().Be(boardId);
        new ListApprovalRequestsQueryValidator().Validate(new ListApprovalRequestsQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }
}
