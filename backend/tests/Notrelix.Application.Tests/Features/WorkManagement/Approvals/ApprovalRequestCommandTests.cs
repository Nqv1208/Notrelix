using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CreateApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.ApproveApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.RejectApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CancelApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.DeleteApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.RestoreApprovalRequest;

namespace Notrelix.Application.Tests.Features.WorkManagement.Approvals;

public class ApprovalRequestCommandTests : WorkManagementHandlerTestBase
{
    // ── CreateApprovalRequest ─────────────────────────────────

    public class CreateApprovalRequestTests : ApprovalRequestCommandTests
    {
        private readonly CreateApprovalRequestCommandHandler _handler;

        public CreateApprovalRequestTests()
        {
            _handler = new CreateApprovalRequestCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesApprovalRequest()
        {
            var board = CreateBoard();
            SetupBoards(board);

            var command = new CreateApprovalRequestCommand(
                board.Id, "Approve design", null, null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_BoardNotFound_ThrowsNotFoundException()
        {
            var command = new CreateApprovalRequestCommand(
                Guid.CreateVersion7(), "Approve design", null, null);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_WithSteps_CreatesRequestWithSteps()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var approverId = Guid.CreateVersion7();

            var steps = new List<ApprovalStepDto>
            {
                new(approverId, null),
                new(null, Guid.CreateVersion7())
            };
            var command = new CreateApprovalRequestCommand(
                board.Id, "Multi-step approval", null, steps);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_WithDescription_CreatesRequestWithDescription()
        {
            var board = CreateBoard();
            SetupBoards(board);

            var command = new CreateApprovalRequestCommand(
                board.Id, "Approve release", "Please review", null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }

    // ── ApproveApprovalRequest ────────────────────────────────

    public class ApproveApprovalRequestTests : ApprovalRequestCommandTests
    {
        private readonly ApproveApprovalRequestCommandHandler _handler;

        public ApproveApprovalRequestTests()
        {
            _handler = new ApproveApprovalRequestCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_PendingRequest_Approves()
        {
            var approvalRequest = CreateApprovalRequest();
            approvalRequest.AddStep(1, TestUserId, TestNow, approverUserId: TestUserId);
            SetupApprovalRequests(approvalRequest);

            var command = new ApproveApprovalRequestCommand(
                approvalRequest.Id, "Looks good", 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsNotFoundException()
        {
            var command = new ApproveApprovalRequestCommand(
                Guid.CreateVersion7(), null, 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyApprovedRequest_ReturnsFailure()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Approved);
            SetupApprovalRequests(approvalRequest);

            var command = new ApproveApprovalRequestCommand(
                approvalRequest.Id, null, 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_AlreadyRejectedRequest_ReturnsFailure()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Rejected);
            SetupApprovalRequests(approvalRequest);

            var command = new ApproveApprovalRequestCommand(
                approvalRequest.Id, null, 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeFalse();
        }
    }

    // ── RejectApprovalRequest ─────────────────────────────────

    public class RejectApprovalRequestTests : ApprovalRequestCommandTests
    {
        private readonly RejectApprovalRequestCommandHandler _handler;

        public RejectApprovalRequestTests()
        {
            _handler = new RejectApprovalRequestCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_PendingRequest_Rejects()
        {
            var approvalRequest = CreateApprovalRequest();
            approvalRequest.AddStep(1, TestUserId, TestNow, approverUserId: TestUserId);
            SetupApprovalRequests(approvalRequest);

            var command = new RejectApprovalRequestCommand(
                approvalRequest.Id, "Needs changes", 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsNotFoundException()
        {
            var command = new RejectApprovalRequestCommand(
                Guid.CreateVersion7(), null, 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyApprovedRequest_ReturnsFailure()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Approved);
            SetupApprovalRequests(approvalRequest);

            var command = new RejectApprovalRequestCommand(
                approvalRequest.Id, null, 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_AlreadyCancelledRequest_ReturnsFailure()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Cancelled);
            SetupApprovalRequests(approvalRequest);

            var command = new RejectApprovalRequestCommand(
                approvalRequest.Id, null, 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeFalse();
        }
    }

    // ── CancelApprovalRequest ─────────────────────────────────

    public class CancelApprovalRequestTests : ApprovalRequestCommandTests
    {
        private readonly Mock<IResourceVisibility> _visibility = new();
        private readonly Mock<IExecutionContextReader> _executionContext = new();
        private readonly Mock<IAccessFactsProvider> _facts = new();
        private readonly Mock<IAccessPolicyEvaluator> _policy = new();
        private readonly CancelApprovalRequestCommandHandler _handler;

        public CancelApprovalRequestTests()
        {
            _executionContext.Setup(c => c.Snapshot).Returns(new ExecutionContextSnapshot(
                TestUserId, TestAccountId, TestWorkspaceId, null,
                ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "corr"));
            _policy.Setup(p => p.Evaluate(It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(), It.IsAny<AccessFacts>(), It.IsAny<object>()))
                .Returns(AccessDecision.Deny(AccessDecisionKind.Forbidden, "no"));
            _handler = new CancelApprovalRequestCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object,
                _visibility.Object,
                _executionContext.Object,
                _facts.Object,
                _policy.Object);
        }

        private void ManagerPolicy() =>
            _policy.Setup(p => p.Evaluate(It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(), It.IsAny<AccessFacts>(), It.IsAny<object>()))
                .Returns(AccessDecision.Allow());

        private ApprovalRequest ItemRequest(Guid boardId, Guid requestedBy)
        {
            var item = CreateBoardItem(boardId: boardId);
            SetupBoardItems(item);
            var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), item.Id, TestWorkspaceId);
            return ApprovalRequest.Create(TestAccountId, TestWorkspaceId, target, "Sign off", requestedBy, TestNow);
        }

        [Fact]
        public async Task Handle_RequesterCancelsOwnPendingRequest_WithoutManageBoard()
        {
            var approvalRequest = CreateApprovalRequest();
            SetupApprovalRequests(approvalRequest);

            var result = await _handler.Handle(new CancelApprovalRequestCommand(approvalRequest.Id, 1), CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            approvalRequest.Status.Should().Be(ApprovalStatus.Cancelled);
        }

        [Fact]
        public async Task Handle_RequesterWhoCanReadTheItemBoard_Cancels()
        {
            var boardId = Guid.CreateVersion7();
            var approvalRequest = ItemRequest(boardId, TestUserId);
            SetupApprovalRequests(approvalRequest);
            _visibility.Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { boardId });

            var result = await _handler.Handle(new CancelApprovalRequestCommand(approvalRequest.Id, 1), CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_RequesterWhoLostTheItemBoard_IsRefusedWithoutManageBoard()
        {
            var approvalRequest = ItemRequest(Guid.CreateVersion7(), TestUserId);
            SetupApprovalRequests(approvalRequest);
            _visibility.Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Guid>());

            await _handler.Invoking(h => h.Handle(new CancelApprovalRequestCommand(approvalRequest.Id, 1), CancellationToken.None))
                .Should().ThrowAsync<ForbiddenException>();
            approvalRequest.Status.Should().Be(ApprovalStatus.Pending);
        }

        [Fact]
        public async Task Handle_OtherMemberWithoutManageBoard_IsRefused()
        {
            var approvalRequest = CreateApprovalRequest();
            SetupApprovalRequests(approvalRequest);
            RequestContextMock.Setup(c => c.UserId).Returns(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(new CancelApprovalRequestCommand(approvalRequest.Id, 1), CancellationToken.None))
                .Should().ThrowAsync<ForbiddenException>();
            approvalRequest.Status.Should().Be(ApprovalStatus.Pending);
        }

        [Fact]
        public async Task Handle_ManagerCancelsAnotherPersonsRequest()
        {
            var approvalRequest = CreateApprovalRequest();
            SetupApprovalRequests(approvalRequest);
            RequestContextMock.Setup(c => c.UserId).Returns(Guid.CreateVersion7());
            ManagerPolicy();

            var result = await _handler.Handle(new CancelApprovalRequestCommand(approvalRequest.Id, 1), CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            approvalRequest.Status.Should().Be(ApprovalStatus.Cancelled);
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsNotFoundException()
        {
            var command = new CancelApprovalRequestCommand(Guid.CreateVersion7(), 1);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Cancelled)]
        public async Task Handle_DecidedRequest_ThrowsBusinessRuleException(ApprovalStatus status)
        {
            var approvalRequest = CreateApprovalRequest(status: status);
            SetupApprovalRequests(approvalRequest);

            await _handler.Invoking(h => h.Handle(new CancelApprovalRequestCommand(approvalRequest.Id, 1), CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }

        [Fact]
        public void Command_CarriesTheClientVersion_BoundToTheApprovalRequest_AndAStaleZeroIsRejected()
        {
            var command = new CancelApprovalRequestCommand(Guid.CreateVersion7(), 3);

            ((IExpectedVersionRequest)command).ExpectedVersion.Should().Be(3);
            ((IExpectedVersionRequest)command).Resource.Kind.Value.Should().Be("work-management.approval-request");
            new CancelApprovalRequestCommandValidator().Validate(command).IsValid.Should().BeTrue();
            new CancelApprovalRequestCommandValidator().Validate(command with { ExpectedVersion = 0 }).IsValid.Should().BeFalse();
        }
    }

    // ── DeleteApprovalRequest ─────────────────────────────

    public class DeleteApprovalRequestTests : ApprovalRequestCommandTests
    {
        private readonly DeleteApprovalRequestCommandHandler _handler;

        public DeleteApprovalRequestTests()
        {
            _handler = new DeleteApprovalRequestCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ActiveRequest_Deletes()
        {
            var approvalRequest = CreateApprovalRequest();
            SetupApprovalRequests(approvalRequest);

            var command = new DeleteApprovalRequestCommand(approvalRequest.Id, 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsNotFoundException()
        {
            var command = new DeleteApprovalRequestCommand(Guid.CreateVersion7(), 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyDeletedRequest_IsIdempotent()
        {
            var approvalRequest = CreateApprovalRequest(isDeleted: true);
            SetupApprovalRequests(approvalRequest);

            var command = new DeleteApprovalRequestCommand(approvalRequest.Id, 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }

    // ── RestoreApprovalRequest ────────────────────────────────

    public class RestoreApprovalRequestTests : ApprovalRequestCommandTests
    {
        private readonly RestoreApprovalRequestCommandHandler _handler;

        public RestoreApprovalRequestTests()
        {
            _handler = new RestoreApprovalRequestCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_DeletedRequest_Restores()
        {
            var approvalRequest = CreateApprovalRequest(isDeleted: true);
            SetupApprovalRequests(approvalRequest);

            var command = new RestoreApprovalRequestCommand(approvalRequest.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsNotFoundException()
        {
            var command = new RestoreApprovalRequestCommand(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_NotDeletedRequest_IsIdempotent()
        {
            var approvalRequest = CreateApprovalRequest();
            SetupApprovalRequests(approvalRequest);

            var command = new RestoreApprovalRequestCommand(approvalRequest.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }
}
