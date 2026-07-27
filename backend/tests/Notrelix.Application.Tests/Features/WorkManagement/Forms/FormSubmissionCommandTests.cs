using Notrelix.Application.Features.WorkManagement.Forms.Commands.ProcessFormSubmission;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RejectFormSubmission;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.MarkFormSubmissionAsSpam;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteFormSubmission;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Application.Tests.Features.WorkManagement.Forms;

public class FormSubmissionCommandTests : WorkManagementHandlerTestBase
{
    // ── ProcessFormSubmission ────────────────────────────────

    public class ProcessFormSubmissionTests : FormSubmissionCommandTests
    {
        private readonly ProcessFormSubmissionCommandHandler _handler;

        public ProcessFormSubmissionTests()
        {
            _handler = new ProcessFormSubmissionCommandHandler(
                DbContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_AcceptedSubmission_Processed()
        {
            var submission = CreateFormSubmission();
            SetupFormSubmissions(submission);

            var command = new ProcessFormSubmissionCommand(submission.Id, Guid.CreateVersion7());

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_SubmissionNotFound_ThrowsNotFoundException()
        {
            var command = new ProcessFormSubmissionCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_RejectedSubmission_ThrowsBusinessRuleException()
        {
            var submission = CreateFormSubmission(status: FormSubmissionStatus.Rejected);
            SetupFormSubmissions(submission);

            var command = new ProcessFormSubmissionCommand(submission.Id, Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }

        [Fact]
        public async Task Handle_SpamSubmission_ThrowsBusinessRuleException()
        {
            var submission = CreateFormSubmission(status: FormSubmissionStatus.Spam);
            SetupFormSubmissions(submission);

            var command = new ProcessFormSubmissionCommand(submission.Id, Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }

        [Fact]
        public async Task Handle_DeletedSubmission_ThrowsBusinessRuleException()
        {
            var submission = CreateFormSubmission(status: FormSubmissionStatus.Deleted);
            SetupFormSubmissions(submission);

            var command = new ProcessFormSubmissionCommand(submission.Id, Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }
    }

    // ── RejectFormSubmission ─────────────────────────────────

    public class RejectFormSubmissionTests : FormSubmissionCommandTests
    {
        private readonly RejectFormSubmissionCommandHandler _handler;

        public RejectFormSubmissionTests()
        {
            _handler = new RejectFormSubmissionCommandHandler(
                DbContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_AcceptedSubmission_Rejected()
        {
            var submission = CreateFormSubmission();
            SetupFormSubmissions(submission);

            var command = new RejectFormSubmissionCommand(submission.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_SubmissionNotFound_ThrowsNotFoundException()
        {
            var command = new RejectFormSubmissionCommand(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyRejectedSubmission_ThrowsBusinessRuleException()
        {
            var submission = CreateFormSubmission(status: FormSubmissionStatus.Rejected);
            SetupFormSubmissions(submission);

            var command = new RejectFormSubmissionCommand(submission.Id);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }
    }

    // ── MarkFormSubmissionAsSpam ─────────────────────────────

    public class MarkFormSubmissionAsSpamTests : FormSubmissionCommandTests
    {
        private readonly MarkFormSubmissionAsSpamCommandHandler _handler;

        public MarkFormSubmissionAsSpamTests()
        {
            _handler = new MarkFormSubmissionAsSpamCommandHandler(
                DbContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_AcceptedSubmission_MarkedAsSpam()
        {
            var submission = CreateFormSubmission();
            SetupFormSubmissions(submission);

            var command = new MarkFormSubmissionAsSpamCommand(submission.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_SubmissionNotFound_ThrowsNotFoundException()
        {
            var command = new MarkFormSubmissionAsSpamCommand(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadySpamSubmission_ThrowsBusinessRuleException()
        {
            var submission = CreateFormSubmission(status: FormSubmissionStatus.Spam);
            SetupFormSubmissions(submission);

            var command = new MarkFormSubmissionAsSpamCommand(submission.Id);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }
    }

    // ── DeleteFormSubmission ─────────────────────────────────

    public class DeleteFormSubmissionTests : FormSubmissionCommandTests
    {
        private readonly DeleteFormSubmissionCommandHandler _handler;

        public DeleteFormSubmissionTests()
        {
            _handler = new DeleteFormSubmissionCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_AcceptedSubmission_Deleted()
        {
            var submission = CreateFormSubmission();
            SetupFormSubmissions(submission);

            var command = new DeleteFormSubmissionCommand(submission.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_SubmissionNotFound_ThrowsNotFoundException()
        {
            var command = new DeleteFormSubmissionCommand(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyDeletedSubmission_ThrowsBusinessRuleException()
        {
            var submission = CreateFormSubmission(status: FormSubmissionStatus.Deleted);
            SetupFormSubmissions(submission);

            var command = new DeleteFormSubmissionCommand(submission.Id);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }
    }
}
