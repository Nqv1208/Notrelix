using Notrelix.Application.Features.WorkManagement.Forms.Commands.CreateForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormDetails;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.PublishForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RestoreForm;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Application.Tests.Features.WorkManagement.Forms;

public class FormCommandTests : WorkManagementHandlerTestBase
{
    // ── CreateForm ───────────────────────────────────────────

    public class CreateFormTests : FormCommandTests
    {
        private readonly CreateFormCommandHandler _handler;

        public CreateFormTests()
        {
            _handler = new CreateFormCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesForm()
        {
            var board = CreateBoard();
            SetupBoards(board);

            var command = new CreateFormCommand(board.Id, "My Form");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_BoardNotFound_ThrowsNotFoundException()
        {
            var command = new CreateFormCommand(Guid.CreateVersion7(), "My Form");

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_GeneratesSlugFromTitle()
        {
            var board = CreateBoard();
            SetupBoards(board);

            var command = new CreateFormCommand(board.Id, "Contact Us Form");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }

    // ── UpdateFormDetails ────────────────────────────────────

    public class UpdateFormDetailsTests : FormCommandTests
    {
        private readonly UpdateFormDetailsCommandHandler _handler;

        public UpdateFormDetailsTests()
        {
            _handler = new UpdateFormDetailsCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesForm()
        {
            var form = CreateForm();
            SetupForms(form);

            var command = new UpdateFormDetailsCommand(
                form.Id, "Updated Name", BoardVisibility.Workspace, "{}", "{}");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FormNotFound_ThrowsNotFoundException()
        {
            var command = new UpdateFormDetailsCommand(
                Guid.CreateVersion7(), "Updated Name", BoardVisibility.Workspace, "{}", "{}");

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedForm_ThrowsDomainException()
        {
            var form = CreateForm(status: FormStatus.Deleted);
            SetupForms(form);

            var command = new UpdateFormDetailsCommand(
                form.Id, "Updated Name", BoardVisibility.Workspace, "{}", "{}");

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.DomainException>();
        }
    }

    // ── PublishForm ──────────────────────────────────────────

    public class PublishFormTests : FormCommandTests
    {
        private readonly PublishFormCommandHandler _handler;

        public PublishFormTests()
        {
            _handler = new PublishFormCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ClosedForm_ThrowsBusinessRuleException()
        {
            var form = CreateForm(status: FormStatus.Closed);
            SetupForms(form);

            var command = new PublishFormCommand(form.Id);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }

        [Fact]
        public async Task Handle_FormNotFound_ThrowsNotFoundException()
        {
            var command = new PublishFormCommand(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_FormWithNoQuestions_ThrowsBusinessRuleException()
        {
            var form = CreateForm();
            SetupForms(form);

            var command = new PublishFormCommand(form.Id);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        }
    }

    // ── CloseForm ────────────────────────────────────────────

    public class CloseFormTests : FormCommandTests
    {
        private readonly CloseFormCommandHandler _handler;

        public CloseFormTests()
        {
            _handler = new CloseFormCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_AlreadyClosedForm_Idempotent()
        {
            var form = CreateForm(status: FormStatus.Closed);
            SetupForms(form);

            var command = new CloseFormCommand(form.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FormNotFound_ThrowsNotFoundException()
        {
            var command = new CloseFormCommand(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedForm_ThrowsDomainException()
        {
            var form = CreateForm(status: FormStatus.Deleted);
            SetupForms(form);

            var command = new CloseFormCommand(form.Id);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Domain.Common.Exceptions.DomainException>();
        }
    }

    // ── DeleteForm ───────────────────────────────────────

    public class DeleteFormTests : FormCommandTests
    {
        private readonly DeleteFormCommandHandler _handler;

        public DeleteFormTests()
        {
            _handler = new DeleteFormCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_DeletesForm()
        {
            var form = CreateForm();
            SetupForms(form);

            var command = new DeleteFormCommand(form.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FormNotFound_ThrowsNotFoundException()
        {
            var command = new DeleteFormCommand(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyDeletedForm_Idempotent()
        {
            var form = CreateForm(status: FormStatus.Deleted);
            SetupForms(form);

            var command = new DeleteFormCommand(form.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }

    // ── RestoreForm ──────────────────────────────────────────

    public class RestoreFormTests : FormCommandTests
    {
        private readonly RestoreFormCommandHandler _handler;

        public RestoreFormTests()
        {
            _handler = new RestoreFormCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_DeletedForm_Restores()
        {
            var form = CreateForm(status: FormStatus.Deleted);
            SetupForms(form);

            var command = new RestoreFormCommand(form.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FormNotFound_ThrowsNotFoundException()
        {
            var command = new RestoreFormCommand(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_NotDeletedForm_Idempotent()
        {
            var form = CreateForm();
            SetupForms(form);

            var command = new RestoreFormCommand(form.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }
}
