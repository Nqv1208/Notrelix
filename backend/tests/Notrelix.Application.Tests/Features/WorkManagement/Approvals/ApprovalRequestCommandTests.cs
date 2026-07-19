using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CreateApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.ApproveApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.RejectApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CancelApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.SoftDeleteApprovalRequest;
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
                board.Id, ResourceType.Board, "Approve design", null, null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_BoardNotFound_ThrowsNotFoundException()
        {
            var command = new CreateApprovalRequestCommand(
                Guid.CreateVersion7(), ResourceType.Board, "Approve design", null, null);

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
                board.Id, ResourceType.Board, "Multi-step approval", null, steps);

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
                board.Id, ResourceType.Board, "Approve release", "Please review", null);

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
        public async Task Handle_AlreadyApprovedRequest_ThrowsBusinessRuleException()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Approved);
            SetupApprovalRequests(approvalRequest);

            var command = new ApproveApprovalRequestCommand(
                approvalRequest.Id, null, 1);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }

        [Fact]
        public async Task Handle_AlreadyRejectedRequest_ThrowsBusinessRuleException()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Rejected);
            SetupApprovalRequests(approvalRequest);

            var command = new ApproveApprovalRequestCommand(
                approvalRequest.Id, null, 1);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
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
        public async Task Handle_AlreadyApprovedRequest_ThrowsBusinessRuleException()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Approved);
            SetupApprovalRequests(approvalRequest);

            var command = new RejectApprovalRequestCommand(
                approvalRequest.Id, null, 1);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }

        [Fact]
        public async Task Handle_AlreadyCancelledRequest_ThrowsBusinessRuleException()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Cancelled);
            SetupApprovalRequests(approvalRequest);

            var command = new RejectApprovalRequestCommand(
                approvalRequest.Id, null, 1);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }
    }

    // ── CancelApprovalRequest ─────────────────────────────────

    public class CancelApprovalRequestTests : ApprovalRequestCommandTests
    {
        private readonly CancelApprovalRequestCommandHandler _handler;

        public CancelApprovalRequestTests()
        {
            _handler = new CancelApprovalRequestCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_PendingRequest_Cancels()
        {
            var approvalRequest = CreateApprovalRequest();
            SetupApprovalRequests(approvalRequest);

            var command = new CancelApprovalRequestCommand(approvalRequest.Id, 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsNotFoundException()
        {
            var command = new CancelApprovalRequestCommand(Guid.CreateVersion7(), 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyApprovedRequest_ThrowsBusinessRuleException()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Approved);
            SetupApprovalRequests(approvalRequest);

            var command = new CancelApprovalRequestCommand(approvalRequest.Id, 1);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }

        [Fact]
        public async Task Handle_AlreadyRejectedRequest_ThrowsBusinessRuleException()
        {
            var approvalRequest = CreateApprovalRequest(status: ApprovalStatus.Rejected);
            SetupApprovalRequests(approvalRequest);

            var command = new CancelApprovalRequestCommand(approvalRequest.Id, 1);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }
    }

    // ── SoftDeleteApprovalRequest ─────────────────────────────

    public class SoftDeleteApprovalRequestTests : ApprovalRequestCommandTests
    {
        private readonly SoftDeleteApprovalRequestCommandHandler _handler;

        public SoftDeleteApprovalRequestTests()
        {
            _handler = new SoftDeleteApprovalRequestCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ActiveRequest_SoftDeletes()
        {
            var approvalRequest = CreateApprovalRequest();
            SetupApprovalRequests(approvalRequest);

            var command = new SoftDeleteApprovalRequestCommand(approvalRequest.Id, 1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsNotFoundException()
        {
            var command = new SoftDeleteApprovalRequestCommand(Guid.CreateVersion7(), 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyDeletedRequest_IsIdempotent()
        {
            var approvalRequest = CreateApprovalRequest(isDeleted: true);
            SetupApprovalRequests(approvalRequest);

            var command = new SoftDeleteApprovalRequestCommand(approvalRequest.Id, 1);

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
