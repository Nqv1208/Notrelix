using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CreateForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.MarkFormSubmissionAsSpam;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.ProcessFormSubmission;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.PublishForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RejectFormSubmission;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RestoreForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormDetails;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Forms;
using AppValidation = Notrelix.Application.Common.Exceptions.ValidationException;
using DomainBusinessRule = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.WorkManagement.Forms;

/// <summary>
/// The write contract of the form use cases that the per-command suites do not pin: what each command
/// needs (action, resource, version, idempotency), the validators, the no-op and failure-atomicity of the
/// lifecycle, the question mapping rule, spam as a soft state, and a question type that cannot be
/// answered yet staying out of the filler's way.
/// </summary>
public class FormWriteContractTests : WorkManagementHandlerTestBase
{
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private FormQuestion Question(Form form, string key, FormQuestionType type, bool required, Guid? fieldId = null) =>
        FormQuestion.Create(TestAccountId, TestWorkspaceId, form.Id, fieldId, key, key, type, required, FractionalIndex.Initial(), FormQuestionConfig.FromConfig(type, null));

    // ── what each command needs ─────────────────────────────────────────────

    [Fact]
    public void FormWrites_NeedTheFormVersion_AndTheManagerAction()
    {
        var id = Guid.CreateVersion7();
        foreach (var versioned in new IExpectedVersionRequest[]
        {
            new UpdateFormDetailsCommand(id, "n", BoardVisibility.Workspace, "{}", "{}", 3),
            new PublishFormCommand(id, 3),
            new CloseFormCommand(id, 3),
            new DeleteFormCommand(id, 3),
        })
        {
            versioned.ExpectedVersion.Should().Be(3, versioned.GetType().Name);
            versioned.Resource.Kind.Value.Should().Be("work-management.form");
            ((IRequirePermission)versioned).Action.Should().Be(PermissionAction.ManageBoard);
        }
    }

    [Fact]
    public void ARestoredForm_HasNoOtherWriterToRaceWith_SoRestoreCarriesNoVersion()
    {
        // A deleted form accepts no other write (every one is NotFound), and restoring twice changes nothing,
        // so restore is the one form write that is not version-guarded.
        typeof(IExpectedVersionRequest).IsAssignableFrom(typeof(RestoreFormCommand)).Should().BeFalse();
        new RestoreFormCommand(Guid.CreateVersion7()).Action.Should().Be(PermissionAction.ManageBoard);
    }

    [Fact]
    public void CreatingAForm_IsAnEditorSchemaChange_AndSubmittingNeedsTheRightToCreateItems()
    {
        new CreateFormCommand(Guid.CreateVersion7(), "Form").Action.Should().Be(PermissionAction.CreateField);
        new SubmitFormCommand(Guid.CreateVersion7(), new()).Action.Should().Be(PermissionAction.CreateItem);
    }

    [Fact]
    public void HandlingAResponse_NeedsTheRightToUpdateItems_NotManagement()
    {
        var id = Guid.CreateVersion7();

        new ProcessFormSubmissionCommand(id, Guid.CreateVersion7()).Action.Should().Be(PermissionAction.UpdateItem);
        new RejectFormSubmissionCommand(id).Action.Should().Be(PermissionAction.UpdateItem);
        new MarkFormSubmissionAsSpamCommand(id).Action.Should().Be(PermissionAction.UpdateItem);
    }

    [Fact]
    public void EveryFormCreateAndSubmitIsIdempotent_UnderItsOwnOperationName()
    {
        foreach (var command in new[] { typeof(CreateFormCommand), typeof(SubmitFormCommand), typeof(ProcessFormSubmissionCommand), typeof(RejectFormSubmissionCommand), typeof(MarkFormSubmissionAsSpamCommand) })
        {
            typeof(IIdempotentRequest).IsAssignableFrom(command).Should().BeTrue(command.Name);
            command.GetCustomAttributes(typeof(IdempotencyOperationAttribute), false).Should().ContainSingle(command.Name);
        }
    }

    // ── validators ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateForm_RefusesAnEmptyOrTooLongTitle()
    {
        var validator = new CreateFormCommandValidator();
        var board = Guid.CreateVersion7();

        validator.Validate(new CreateFormCommand(board, "")).IsValid.Should().BeFalse();
        validator.Validate(new CreateFormCommand(board, new string('x', 201))).IsValid.Should().BeFalse();
        validator.Validate(new CreateFormCommand(board, new string('x', 200))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectingAResponse_TakesAReasonOfAtMost500Characters()
    {
        var validator = new RejectFormSubmissionCommandValidator();
        var id = Guid.CreateVersion7();

        validator.Validate(new RejectFormSubmissionCommand(id, new string('x', 501))).IsValid.Should().BeFalse();
        validator.Validate(new RejectFormSubmissionCommand(id, new string('x', 500))).IsValid.Should().BeTrue();
        validator.Validate(new RejectFormSubmissionCommand(id, null)).IsValid.Should().BeTrue();
    }

    // ── create ──────────────────────────────────────────────────────────────

    private List<Form> CaptureAddedForms()
    {
        var added = new List<Form>();
        var set = CreateAsyncDbSet(added);
        Mock.Get(set).Setup(s => s.Add(It.IsAny<Form>())).Callback<Form>(added.Add);
        DbContextMock.Setup(c => c.Forms).Returns(set);
        return added;
    }

    [Fact]
    public async Task CreatingTwoFormsWithTheSameTitle_GivesEachItsOwnSlug()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var added = CaptureAddedForms();
        var handler = new CreateFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        await handler.Handle(new CreateFormCommand(board.Id, "IT support"), CancellationToken.None);
        await handler.Handle(new CreateFormCommand(board.Id, "IT support"), CancellationToken.None);

        added.Select(f => f.Slug).Distinct().Should().HaveCount(2, "the slug column is unique across every tenant");
        added.Should().OnlyContain(f => f.Status == FormStatus.Draft && f.Name == "IT support");
    }

    [Fact]
    public async Task ANewForm_IsADraftOnTheBoardsWorkspace_AndRaisesOneCreatedEvent()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var added = CaptureAddedForms();
        var handler = new CreateFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        var result = await handler.Handle(new CreateFormCommand(board.Id, "Request"), CancellationToken.None);

        var form = added.Single();
        result.Data.Should().Be(form.Id);
        form.Status.Should().Be(FormStatus.Draft, "a new form is invisible to submitters");
        form.WorkspaceId.Should().Be(board.WorkspaceId);
        form.BoardId.Should().Be(board.Id);
        form.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task ANewForm_KeepsItsDescriptionInTheSettings_AndABlankOneLeavesTheSettingsAlone()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var added = CaptureAddedForms();
        var handler = new CreateFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        await handler.Handle(new CreateFormCommand(board.Id, "Request", Description: "  Ask IT for help  "), CancellationToken.None);
        await handler.Handle(new CreateFormCommand(board.Id, "Blank", Description: "  "), CancellationToken.None);

        JsonDocument.Parse(added[0].SettingsJson).RootElement.GetProperty("description").GetString().Should().Be("Ask IT for help");
        added[1].SettingsJson.Should().Be("{}");
        new CreateFormCommandValidator().Validate(new CreateFormCommand(board.Id, "x", Description: new string('x', 2001))).IsValid.Should().BeFalse();
    }

    // ── lifecycle ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Publish_RefusesAFormWhoseMappedFieldWasDeleted_AndKeepsItADraft()
    {
        var form = CreateForm();
        var gone = BoardField.Create(TestAccountId, TestWorkspaceId, form.BoardId, "Gone", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), TestUserId, TestNow);
        form.AddQuestion(Question(form, "title", FormQuestionType.ShortText, true, gone.Id), TestUserId, TestNow);
        SetupForms(form);
        // The field no longer exists on the board: nothing is returned for it.
        SetupBoardFields();
        var before = form.Version;
        var handler = new PublishFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        var act = () => handler.Handle(new PublishFormCommand(form.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainBusinessRule>()).Which.RuleCode
            .Should().Be(WorkManagementRuleCodes.WorkManagement_Form_QuestionFieldRemoved);
        form.Status.Should().Be(FormStatus.Draft);
        form.Version.Should().Be(before);
    }

    [Fact]
    public async Task Publish_AFormWithQuestions_PublishesOnce_AndAgainChangesNothing()
    {
        var form = CreateForm();
        form.AddQuestion(Question(form, "title", FormQuestionType.ShortText, true), TestUserId, TestNow);
        SetupForms(form);
        var handler = new PublishFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        await handler.Handle(new PublishFormCommand(form.Id), CancellationToken.None);
        var published = form.Version;
        ((IHasDomainEvents)form).ClearDomainEvents();
        await handler.Handle(new PublishFormCommand(form.Id), CancellationToken.None);

        form.Status.Should().Be(FormStatus.Published);
        form.Version.Should().Be(published, "publishing a published form is a no-op");
        form.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Close_APublishedForm_ClosesIt_AndASubmitterIsThenRefused()
    {
        var form = CreateForm();
        form.AddQuestion(Question(form, "title", FormQuestionType.ShortText, false), TestUserId, TestNow);
        form.Publish(TestUserId, TestNow);
        SetupForms(form);

        await new CloseFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new CloseFormCommand(form.Id), CancellationToken.None);

        form.Status.Should().Be(FormStatus.Closed);
        var submit = new SubmitFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
        var act = () => submit.Handle(new SubmitFormCommand(form.Id, new()), CancellationToken.None);
        (await act.Should().ThrowAsync<DomainBusinessRule>()).Which.RuleCode
            .Should().Be(WorkManagementRuleCodes.WorkManagement_Form_CannotSubmitToClosed);
    }

    [Fact]
    public async Task DeleteThenRestore_BringsTheFormBackWithItsQuestions_AndRepeatingEitherChangesNothing()
    {
        var form = CreateForm();
        form.AddQuestion(Question(form, "title", FormQuestionType.ShortText, true), TestUserId, TestNow);
        SetupForms(form);
        var delete = new DeleteFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
        var restore = new RestoreFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        await delete.Handle(new DeleteFormCommand(form.Id), CancellationToken.None);
        form.IsDeleted.Should().BeTrue();
        var deleted = form.Version;
        await delete.Handle(new DeleteFormCommand(form.Id), CancellationToken.None);
        form.Version.Should().Be(deleted, "deleting a deleted form is a no-op");

        await restore.Handle(new RestoreFormCommand(form.Id), CancellationToken.None);
        form.IsDeleted.Should().BeFalse();
        form.Questions.Should().ContainSingle();
        var restored = form.Version;
        await restore.Handle(new RestoreFormCommand(form.Id), CancellationToken.None);
        form.Version.Should().Be(restored, "restoring a live form is a no-op");
    }

    [Fact]
    public async Task UpdateDetails_WithWhatTheFormAlreadyHolds_ChangesNothing()
    {
        var form = CreateForm();
        SetupForms(form);
        var before = form.Version;
        ((IHasDomainEvents)form).ClearDomainEvents();
        var handler = new UpdateFormDetailsCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        await handler.Handle(new UpdateFormDetailsCommand(form.Id, form.Name, form.Visibility, form.SettingsJson, form.SubmitterPolicyJson), CancellationToken.None);

        form.Version.Should().Be(before);
        form.DomainEvents.Should().BeEmpty();
    }

    // ── submit ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_AQuestionThatCannotBeAnsweredYet_IsHiddenFromTheFiller_AndNeverBlocksTheForm()
    {
        var form = CreateForm();
        form.AddQuestion(Question(form, "title", FormQuestionType.ShortText, true), TestUserId, TestNow);
        // Old data: a required file question, created before uploads were refused.
        form.AddQuestion(Question(form, "attachment", FormQuestionType.FileUpload, true), TestUserId, TestNow);
        form.Publish(TestUserId, TestNow);
        SetupForms(form);
        var set = CreateAsyncDbSet(new List<FormSubmission>());
        DbContextMock.Setup(c => c.FormSubmissions).Returns(set);
        var handler = new SubmitFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        var result = await handler.Handle(new SubmitFormCommand(form.Id, new() { ["title"] = Json("\"Mua ghế\"") }), CancellationToken.None);

        result.Succeeded.Should().BeTrue("the filler cannot answer a file question, so it is not required of them");
        Mock.Get(set).Verify(s => s.Add(It.IsAny<FormSubmission>()), Times.Once);
    }

    [Fact]
    public async Task Submit_AnAnswerToAQuestionThatCannotBeAnsweredYet_IsRefusedAsUnknown()
    {
        var form = CreateForm();
        form.AddQuestion(Question(form, "title", FormQuestionType.ShortText, true), TestUserId, TestNow);
        form.AddQuestion(Question(form, "attachment", FormQuestionType.Signature, false), TestUserId, TestNow);
        form.Publish(TestUserId, TestNow);
        SetupForms(form);
        var handler = new SubmitFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        var act = () => handler.Handle(new SubmitFormCommand(form.Id, new() { ["title"] = Json("\"x\""), ["attachment"] = Json("\"data\"") }), CancellationToken.None);

        (await act.Should().ThrowAsync<AppValidation>()).Which.Errors.Should().ContainKey("attachment");
    }

    [Fact]
    public async Task Submit_RefusedAnswers_StoreNothing()
    {
        var form = CreateForm();
        form.AddQuestion(Question(form, "title", FormQuestionType.ShortText, true), TestUserId, TestNow);
        form.Publish(TestUserId, TestNow);
        SetupForms(form);
        var set = CreateAsyncDbSet(new List<FormSubmission>());
        DbContextMock.Setup(c => c.FormSubmissions).Returns(set);
        var handler = new SubmitFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        var act = () => handler.Handle(new SubmitFormCommand(form.Id, new()), CancellationToken.None);

        await act.Should().ThrowAsync<AppValidation>();
        Mock.Get(set).Verify(s => s.Add(It.IsAny<FormSubmission>()), Times.Never);
    }

    // ── a response ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Spam_IsASoftState_TheResponseStaysAndIsNeverProcessedOrRejected()
    {
        var submission = CreateFormSubmission();
        var set = CreateAsyncDbSet(new List<FormSubmission> { submission });
        DbContextMock.Setup(c => c.FormSubmissions).Returns(set);

        await new MarkFormSubmissionAsSpamCommandHandler(DbContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new MarkFormSubmissionAsSpamCommand(submission.Id), CancellationToken.None);

        submission.Status.Should().Be(FormSubmissionStatus.Spam);
        submission.PayloadJson.Should().Be("{}", "the answers are kept");
        submission.CreatedItemId.Should().BeNull("spam creates no item");
        Mock.Get(set).Verify(s => s.Remove(It.IsAny<FormSubmission>()), Times.Never, "nothing deletes a spam response");

        var item = CreateBoardItem(boardId: submission.BoardId);
        SetupBoardItems(item);
        var processAct = () => new ProcessFormSubmissionCommandHandler(DbContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new ProcessFormSubmissionCommand(submission.Id, item.Id), CancellationToken.None);
        (await processAct.Should().ThrowAsync<DomainBusinessRule>()).Which.RuleCode
            .Should().Be(WorkManagementRuleCodes.WorkManagement_FormSubmission_CannotProcessUnlessAccepted);
        var rejectAct = () => new RejectFormSubmissionCommandHandler(DbContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new RejectFormSubmissionCommand(submission.Id, "x"), CancellationToken.None);
        await rejectAct.Should().ThrowAsync<DomainBusinessRule>();
        submission.Status.Should().Be(FormSubmissionStatus.Spam);
    }

    [Fact]
    public async Task Reject_KeepsTheTrimmedReason_AndCreatesNoItem()
    {
        var submission = CreateFormSubmission();
        SetupFormSubmissions(submission);

        await new RejectFormSubmissionCommandHandler(DbContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new RejectFormSubmissionCommand(submission.Id, "  Duplicate of ERP-168  "), CancellationToken.None);

        submission.Status.Should().Be(FormSubmissionStatus.Rejected);
        submission.RejectionReason.Should().Be("Duplicate of ERP-168");
        submission.CreatedItemId.Should().BeNull();
        submission.ProcessedAt.Should().Be(TestNow);
    }

    [Fact]
    public async Task Process_TwiceWithTheSameItem_IsANoOp_AndWithAnotherItemIsRefused()
    {
        var submission = CreateFormSubmission();
        SetupFormSubmissions(submission);
        var first = CreateBoardItem(boardId: submission.BoardId);
        var second = CreateBoardItem(boardId: submission.BoardId);
        SetupBoardItems(first, second);
        var handler = new ProcessFormSubmissionCommandHandler(DbContextMock.Object, DateTimeProviderMock.Object);

        await handler.Handle(new ProcessFormSubmissionCommand(submission.Id, first.Id), CancellationToken.None);
        ((IHasDomainEvents)submission).ClearDomainEvents();
        await handler.Handle(new ProcessFormSubmissionCommand(submission.Id, first.Id), CancellationToken.None);
        submission.DomainEvents.Should().BeEmpty("the retry of the same link emits nothing");

        var other = () => handler.Handle(new ProcessFormSubmissionCommand(submission.Id, second.Id), CancellationToken.None);
        (await other.Should().ThrowAsync<DomainBusinessRule>()).Which.RuleCode
            .Should().Be(WorkManagementRuleCodes.WorkManagement_FormSubmission_AlreadyProcessed);
        submission.CreatedItemId.Should().Be(first.Id, "the first item is not replaced");
    }
}
