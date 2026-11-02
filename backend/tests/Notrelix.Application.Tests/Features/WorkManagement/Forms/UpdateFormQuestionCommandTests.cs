using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormQuestion;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Forms;
using DomainBusinessRule = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.WorkManagement.Forms;

/// <summary>
/// UpdateFormQuestion changes one question of a form through the Form, so the form's version moves
/// only when the question did: success, no-op, each rejection (missing form, deleted form, missing
/// question, missing or incompatible field, closed form) and the validator.
/// </summary>
public class UpdateFormQuestionCommandTests : WorkManagementHandlerTestBase
{
    private readonly UpdateFormQuestionCommandHandler _handler;
    private readonly UpdateFormQuestionCommandValidator _validator = new();

    public UpdateFormQuestionCommandTests()
    {
        _handler = new UpdateFormQuestionCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
    }

    private (Form Form, FormQuestion Question) FormWithQuestion(FormQuestionType type = FormQuestionType.ShortText, Guid? fieldId = null)
    {
        var form = CreateForm();
        var question = FormQuestion.Create(TestAccountId, TestWorkspaceId, form.Id, fieldId, "title", "Title", type, true,
            FractionalIndex.Create("a0"), FormQuestionConfig.FromConfig(type, null));
        form.AddQuestion(question, TestUserId, TestNow);
        SetupForms(form);
        return (form, question);
    }

    private BoardField FieldOn(Form form, FieldType type)
    {
        var field = BoardField.Create(TestAccountId, TestWorkspaceId, form.BoardId, "Field", type, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoardFields(field);
        return field;
    }

    [Fact]
    public async Task Handle_ChangedLabelAndRequired_UpdatesTheQuestion_AndMovesTheFormVersionOnce()
    {
        var (form, question) = FormWithQuestion();
        var before = form.Version;

        var result = await _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "What do you need?", false, null, null, ExpectedVersion: before), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        question.Label.Should().Be("What do you need?");
        question.IsRequired.Should().BeFalse();
        form.Version.Should().Be(before + 1);
    }

    [Fact]
    public async Task Handle_SameContent_ChangesNothing_AndMovesNoVersion()
    {
        var (form, question) = FormWithQuestion();
        var before = form.Version;
        var questionVersion = question.Version;
        ((IHasDomainEvents)form).ClearDomainEvents();

        var result = await _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Title", true, null, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        form.Version.Should().Be(before, "a semantic no-op bumps no version");
        question.Version.Should().Be(questionVersion);
        form.DomainEvents.Should().BeEmpty("a semantic no-op emits nothing");
    }

    [Fact]
    public async Task Handle_NewPosition_ReordersTheQuestion()
    {
        var (form, question) = FormWithQuestion();

        await _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Title", true, null, "a5"), CancellationToken.None);

        question.Position.Value.Should().Be("a5");
    }

    [Fact]
    public async Task Handle_CompatibleField_MapsTheQuestionToIt()
    {
        var (form, question) = FormWithQuestion(FormQuestionType.Number);
        var field = FieldOn(form, FieldType.Number);

        await _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Title", true, null, null, field.Id), CancellationToken.None);

        question.BoardFieldId.Should().Be(field.Id);
    }

    [Fact]
    public async Task Handle_NoField_UnmapsTheQuestion()
    {
        var (form, question) = FormWithQuestion(FormQuestionType.Number);
        var field = FieldOn(form, FieldType.Number);
        await _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Title", true, null, null, field.Id), CancellationToken.None);

        await _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Title", true, null, null, null), CancellationToken.None);

        question.BoardFieldId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_IncompatibleField_IsRefused_AndTheQuestionKeepsItsMapping()
    {
        var (form, question) = FormWithQuestion(FormQuestionType.ShortText);
        var number = FieldOn(form, FieldType.Number);
        var before = form.Version;

        var act = () => _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Renamed", true, null, null, number.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainBusinessRule>()).Which.RuleCode
            .Should().Be(WorkManagementRuleCodes.WorkManagement_Form_QuestionFieldMismatch);
        question.BoardFieldId.Should().BeNull();
        question.Label.Should().Be("Title", "a refused update changes nothing");
        form.Version.Should().Be(before);
    }

    [Fact]
    public async Task Handle_FieldOfAnotherBoard_IsNotFound()
    {
        var (form, question) = FormWithQuestion();
        var foreign = BoardField.Create(TestAccountId, TestWorkspaceId, Guid.CreateVersion7(), "Other board", FieldType.Text, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoardFields(foreign);

        var act = () => _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Title", true, null, null, foreign.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnknownField_IsNotFound()
    {
        var (form, question) = FormWithQuestion();

        var act = () => _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Title", true, null, null, Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnknownForm_IsNotFound()
    {
        var act = () => _handler.Handle(new UpdateFormQuestionCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), "Title", true, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedForm_IsNotFound()
    {
        var (form, question) = FormWithQuestion();
        form.Delete(TestUserId, TestNow);

        var act = () => _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Title", true, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_QuestionOfAnotherForm_IsNotFound()
    {
        var (form, _) = FormWithQuestion();

        var act = () => _handler.Handle(new UpdateFormQuestionCommand(form.Id, Guid.CreateVersion7(), "Title", true, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ClosedForm_IsRefused_AndChangesNothing()
    {
        var (form, question) = FormWithQuestion();
        form.Close(TestUserId, TestNow);
        var before = form.Version;

        var act = () => _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Renamed", true, null, null), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainBusinessRule>()).Which.RuleCode
            .Should().Be(WorkManagementRuleCodes.WorkManagement_Form_CannotAddQuestionToClosed);
        question.Label.Should().Be("Title");
        form.Version.Should().Be(before);
    }

    [Fact]
    public async Task Handle_ConfigOfAnotherType_IsRefused()
    {
        var (form, question) = FormWithQuestion(FormQuestionType.ShortText);

        var act = () => _handler.Handle(new UpdateFormQuestionCommand(form.Id, question.Id, "Title", true, "{\"minValue\":1}", null), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainBusinessRule>()).Which.RuleCode
            .Should().Be(WorkManagementRuleCodes.WorkManagement_FormQuestion_MinMaxInvalidForType);
    }

    [Fact]
    public void Validator_RefusesAnEmptyOrTooLongLabel()
    {
        var form = Guid.CreateVersion7();
        var question = Guid.CreateVersion7();

        _validator.Validate(new UpdateFormQuestionCommand(form, question, "", true, null, null)).IsValid.Should().BeFalse();
        _validator.Validate(new UpdateFormQuestionCommand(form, question, new string('x', 501), true, null, null)).IsValid.Should().BeFalse();
        _validator.Validate(new UpdateFormQuestionCommand(form, question, new string('x', 500), true, null, null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Command_NamesTheFormVersionItNeeds_AndTheManagerAction()
    {
        var command = new UpdateFormQuestionCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), "Title", true, null, null, ExpectedVersion: 7);

        ((IExpectedVersionRequest)command).ExpectedVersion.Should().Be(7);
        ((IExpectedVersionRequest)command).Resource.Kind.Value.Should().Be("work-management.form");
        command.Action.Should().Be(PermissionAction.ManageBoard);
    }
}
