using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.DecideApproval;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.RequestItemApproval;

namespace Notrelix.Application.Tests.Features.WorkManagement.Approvals;

/// <summary>
/// Asking one person to approve an Item, and deciding it: only the named approver decides, once, and a person
/// who cannot read the Item's Board can neither be asked nor decide.
/// </summary>
public class ItemApprovalTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IResourceVisibility> _visibility = new();
    private readonly Board _board;
    private readonly BoardItem _item;
    private readonly Guid _approver = Guid.CreateVersion7();

    public ItemApprovalTests()
    {
        _board = CreateBoard();
        _item = CreateBoardItem(boardId: _board.Id);
        SetupBoards(_board);
        SetupBoardItems(_item);
        SetupApprovalRequests();
        RequestContextMock.Setup(c => c.UserId).Returns(TestUserId);
        RequestContextMock.Setup(c => c.RequireAccountId()).Returns(TestAccountId);
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow);
        _visibility
            .Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_board.Id]);
    }

    private async Task<ApprovalRequest> Requested()
    {
        var created = await Request(_approver);
        created.Succeeded.Should().BeTrue();
        return DbContextMock.Object.ApprovalRequests.Single(r => r.Id == created.Data!.Id);
    }

    private Task<Result<ApprovalRequestIdDto>> Request(Guid approver, string? note = null) =>
        new RequestItemApprovalCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _visibility.Object)
            .Handle(new RequestItemApprovalCommand(_item.Id, approver, note), CancellationToken.None);

    private Task<Result> Decide(Guid caller, ApprovalRequest request, string decision, string? comment = null)
    {
        RequestContextMock.Setup(c => c.UserId).Returns(caller);
        return new DecideApprovalCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _visibility.Object)
            .Handle(new DecideApprovalCommand(TestWorkspaceId, request.Id, decision, comment), CancellationToken.None);
    }

    [Fact]
    public async Task Request_CreatesAPendingRequestWithOneStepNamingTheApprover()
    {
        var result = await Request(_approver, "  Please look  ");

        result.Succeeded.Should().BeTrue();
        var request = DbContextMock.Object.ApprovalRequests.Should().ContainSingle().Subject;
        request.Target.ResourceId.Should().Be(_item.Id);
        request.Description.Should().Be("Please look");
        request.Status.Should().Be(ApprovalStatus.Pending);
        request.Steps.Should().ContainSingle(s => s.ApproverUserId == _approver);
    }

    [Fact]
    public async Task Request_NamingYourself_IsRefused()
    {
        var result = await Request(TestUserId);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "work.approval.approver-is-requester");
    }

    [Fact]
    public async Task Request_ForSomeoneWhoCannotReadTheBoard_IsRefused()
    {
        _visibility
            .Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, _approver, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await Request(_approver);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "work.approval.approver-cannot-read-item");
    }

    [Fact]
    public async Task Request_ForAnItemThatDoesNotExist_IsNotFound()
    {
        var handler = new RequestItemApprovalCommandHandler(
            DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _visibility.Object);

        await handler.Invoking(h => h.Handle(new RequestItemApprovalCommand(Guid.CreateVersion7(), _approver, null), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Decide_Approve_ByTheNamedApprover_ApprovesWithTheComment()
    {
        var request = await Requested();

        var result = await Decide(_approver, request, "approve", "  Looks good ");

        result.Succeeded.Should().BeTrue();
        request.Status.Should().Be(ApprovalStatus.Approved);
        request.Steps.Single().Note.Should().Be("Looks good");
    }

    [Fact]
    public async Task Decide_Reject_RejectsTheRequest()
    {
        var request = await Requested();

        (await Decide(_approver, request, "reject")).Succeeded.Should().BeTrue();

        request.Status.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public async Task Decide_ByAnyoneElse_IsForbiddenAndChangesNothing()
    {
        var request = await Requested();
        var version = request.Version;

        await FluentActions.Awaiting(() => Decide(Guid.CreateVersion7(), request, "approve"))
            .Should().ThrowAsync<ForbiddenException>();

        request.Status.Should().Be(ApprovalStatus.Pending);
        request.Version.Should().Be(version);
    }

    [Fact]
    public async Task Decide_Twice_IsAConflict()
    {
        var request = await Requested();
        (await Decide(_approver, request, "approve")).Succeeded.Should().BeTrue();
        var version = request.Version;

        var second = await Decide(_approver, request, "reject");

        second.Succeeded.Should().BeFalse();
        second.TypedErrors.Should().ContainSingle(e => e.Code == "work.approval.already-decided");
        request.Status.Should().Be(ApprovalStatus.Approved);
        request.Version.Should().Be(version);
    }

    [Fact]
    public async Task Decide_WhenTheApproverCanNoLongerReadTheBoard_IsForbidden()
    {
        var request = await Requested();
        _visibility
            .Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, _approver, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await FluentActions.Awaiting(() => Decide(_approver, request, "approve"))
            .Should().ThrowAsync<ForbiddenException>();

        request.Status.Should().Be(ApprovalStatus.Pending);
    }

    [Fact]
    public async Task Decide_RaisesTheFactsTheRequesterAndApproverAreToldAbout()
    {
        var request = await Requested();
        request.DomainEvents.Should().ContainSingle(e => e is Notrelix.Domain.WorkManagement.Approvals.Events.ApprovalStepAssignedDomainEvent);

        await Decide(_approver, request, "approve");

        request.DomainEvents.OfType<Notrelix.Domain.WorkManagement.Approvals.Events.ApprovalRequestApprovedDomainEvent>()
            .Should().ContainSingle(e => e.RequestedBy == TestUserId && e.Target!.ResourceId == _item.Id);
    }
}
