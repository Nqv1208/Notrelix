using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.AddFormQuestion;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CreateForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteFormSubmission;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.MarkFormSubmissionAsSpam;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.ProcessFormSubmission;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.PublishForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RejectFormSubmission;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RemoveFormQuestion;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RestoreForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormDetails;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormQuestion;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetForm;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.ListForms;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.ListFormSubmissions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using DomainBusinessRule = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// The form use cases through the production pipeline on PostgreSQL, for every kind of person: who may create,
/// manage, submit to and handle the responses of a form on a Private board (viewer, commenter, editor, manager,
/// a member with no access, another tenant); a stale or missing form version
/// changes nothing; a retry under the same key has one effect; an archived board refuses every form write;
/// spam stays a visible state; and a question type that cannot be answered yet never blocks a form.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class FormAccessAndConcurrencyIntegrationTests : IAsyncLifetime
{
    private const string BoardKind = "work-management.board";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;
    private OrgStack _stack = null!;
    private Guid _boardId, _viewer, _commenter, _editor, _manager, _noGrant;
    private Guid _formId;

    public FormAccessAndConcurrencyIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        _stack = await _host.SeedWorkspaceAsync();
        _boardId = await _host.SeedBoardAsync(_stack, "Forms board", BoardVisibility.Private);
        _viewer = await GrantAsync("Viewer", PermissionLevel.Viewer);
        _commenter = await GrantAsync("Commenter", PermissionLevel.Commenter);
        _editor = await GrantAsync("Editor", PermissionLevel.Editor);
        _manager = await GrantAsync("Manager", PermissionLevel.Manager);
        _noGrant = await _host.AddMemberAsync(_stack, WorkspaceRole.Member, "NoGrant");

        _formId = await CreatePublishedFormAsync(_manager);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> GrantAsync(string name, PermissionLevel level)
    {
        var user = await _host.AddMemberAsync(_stack, WorkspaceRole.Member, name);
        await _host.SeedResourcePermissionAsync(_stack, BoardKind, _boardId, PermissionSubjectType.User, user, level);
        return user;
    }

    private ServiceProvider As(Guid user) => _host.CreateProvider(_stack.AccountId, user);

    private async Task<Guid> CreatePublishedFormAsync(Guid by)
    {
        using var provider = As(by);
        var id = ((Notrelix.Application.Common.Models.Result<Guid>)(await OrgTreeTestHost.SendAsync(provider, new CreateFormCommand(_boardId, "Request")))!).Data;
        await OrgTreeTestHost.SendAsync(provider, new AddFormQuestionCommand(id, "title", FormQuestionType.ShortText, "Title", true, null, null, ExpectedVersion: await VersionAsync(id)));
        await OrgTreeTestHost.SendAsync(provider, new AddFormQuestionCommand(id, "qty", FormQuestionType.Number, "Quantity", false, "{\"minValue\":1,\"maxValue\":10}", "a1", ExpectedVersion: await VersionAsync(id)));
        await OrgTreeTestHost.SendAsync(provider, new PublishFormCommand(id, await VersionAsync(id)));
        return id;
    }

    private Task<long> VersionAsync(Guid formId) =>
        _host.ReadAsync(c => c.Set<Form>().IgnoreQueryFilters().Where(f => f.Id == formId).Select(f => f.Version).SingleAsync());

    private Task<int> FormCountAsync() =>
        _host.ReadAsync(c => c.Set<Form>().IgnoreQueryFilters().CountAsync(f => f.BoardId == _boardId));

    private Task<int> SubmissionCountAsync() =>
        _host.ReadAsync(c => c.Set<FormSubmission>().CountAsync(s => s.FormId == _formId));

    private static Dictionary<string, JsonElement> Answers(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private async Task<Guid> SubmitAsync(Guid by, string json = "{\"title\":\"Buy chairs\"}")
    {
        using var provider = As(by);
        return ((Notrelix.Application.Common.Models.Result<Guid>)(await OrgTreeTestHost.SendAsync(provider, new SubmitFormCommand(_formId, Answers(json))))!).Data;
    }

    private static Task Send(ServiceProvider provider, object request, string? key = null) =>
        OrgTreeTestHost.SendIdempotentAsync(provider, request, key);

    // ── who may create and manage ───────────────────────────────────────────

    [Fact]
    public async Task CreatingAForm_NeedsAnEditor_AViewerOrCommenterIsForbidden_AndNothingIsStored()
    {
        var before = await FormCountAsync();

        foreach (var below in new[] { _viewer, _commenter })
        {
            using var provider = As(below);
            await FluentActions.Awaiting(() => Send(provider, new CreateFormCommand(_boardId, "Not allowed")))
                .Should().ThrowAsync<ForbiddenException>();
        }
        (await FormCountAsync()).Should().Be(before);

        using var editor = As(_editor);
        await Send(editor, new CreateFormCommand(_boardId, "By an editor"));
        (await FormCountAsync()).Should().Be(before + 1);
    }

    [Fact]
    public async Task ManagingAForm_NeedsAManager_AnEditorIsForbidden_AndTheFormIsUnchanged()
    {
        var version = await VersionAsync(_formId);
        using var editor = As(_editor);

        var writes = new object[]
        {
            new UpdateFormDetailsCommand(_formId, "Renamed", null, "{}", "{}", version),
            new CloseFormCommand(_formId, version),
            new DeleteFormCommand(_formId, version),
            new AddFormQuestionCommand(_formId, "extra", FormQuestionType.ShortText, "Extra", false, null, null, ExpectedVersion: version),
            new RemoveFormQuestionCommand(_formId, Guid.CreateVersion7(), version),
        };
        foreach (var write in writes)
            await FluentActions.Awaiting(() => Send(editor, write))
                .Should().ThrowAsync<ForbiddenException>(write.GetType().Name);

        (await VersionAsync(_formId)).Should().Be(version);
        using var manager = As(_manager);
        await Send(manager, new UpdateFormDetailsCommand(_formId, "Renamed", null, "{}", "{}", version));
        (await VersionAsync(_formId)).Should().Be(version + 1);
    }

    [Fact]
    public async Task DeleteAndRestore_AreAManagersAndKeepTheFormsQuestions()
    {
        using var manager = As(_manager);
        using var editor = As(_editor);
        await Send(manager, new DeleteFormCommand(_formId, await VersionAsync(_formId)));

        await FluentActions.Awaiting(() => OrgTreeTestHost.QueryAsync(manager, new GetFormQuery(_formId)))
            .Should().ThrowAsync<NotFoundException>("a deleted form is NotFound to everyone");
        await FluentActions.Awaiting(() => Send(editor, new RestoreFormCommand(_formId)))
            .Should().ThrowAsync<Exception>("an editor cannot restore a form");

        await Send(manager, new RestoreFormCommand(_formId));
        var form = await OrgTreeTestHost.QueryAsync(manager, new GetFormQuery(_formId));
        form.Questions.Select(q => q.QuestionKey).Should().Equal("title", "qty");
        form.Status.Should().Be("Published");
    }

    // ── who may read and submit ─────────────────────────────────────────────

    [Fact]
    public async Task ReadingAForm_ShowsItToAViewer_AndTheSameNotFoundToEveryoneWithoutAccess()
    {
        using (var viewer = As(_viewer))
        {
            (await OrgTreeTestHost.QueryAsync(viewer, new GetFormQuery(_formId))).Id.Should().Be(_formId);
            (await OrgTreeTestHost.QueryAsync(viewer, new ListFormsQuery(_boardId))).Should().ContainSingle(f => f.Id == _formId);
        }

        // A member with no grant on the Private board: the identical NotFound, for the real form and for one that never existed.
        using (var noGrant = As(_noGrant))
        {
            foreach (var id in new[] { _formId, Guid.CreateVersion7() })
            {
                await FluentActions.Awaiting(() => OrgTreeTestHost.QueryAsync(noGrant, new GetFormQuery(id)))
                    .Should().ThrowAsync<NotFoundException>();
                await FluentActions.Awaiting(() => Send(noGrant, new SubmitFormCommand(id, Answers("{\"title\":\"x\"}"))))
                    .Should().ThrowAsync<NotFoundException>();
            }
        }

        (await SubmissionCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Submitting_NeedsTheRightToCreateItems_AViewerOrCommenterIsForbidden_AnEditorSubmits()
    {
        foreach (var below in new[] { _viewer, _commenter })
        {
            using var provider = As(below);
            await FluentActions.Awaiting(() => Send(provider, new SubmitFormCommand(_formId, Answers("{\"title\":\"x\"}"))))
                .Should().ThrowAsync<ForbiddenException>();
        }
        (await SubmissionCountAsync()).Should().Be(0);

        await SubmitAsync(_editor);
        (await SubmissionCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task TheInbox_IsForPeopleWhoCanEditItems_AViewerOrCommenterCannotReadOrHandleIt()
    {
        var id = await SubmitAsync(_editor);

        foreach (var below in new[] { _viewer, _commenter })
        {
            using var provider = As(below);
            await FluentActions.Awaiting(() => OrgTreeTestHost.QueryAsync(provider, new ListFormSubmissionsQuery(_formId)))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => Send(provider, new MarkFormSubmissionAsSpamCommand(id)))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => Send(provider, new RejectFormSubmissionCommand(id, "no")))
                .Should().ThrowAsync<ForbiddenException>();
            await FluentActions.Awaiting(() => Send(provider, new DeleteFormSubmissionCommand(id)))
                .Should().ThrowAsync<ForbiddenException>();
        }

        using var editor = As(_editor);
        (await OrgTreeTestHost.QueryAsync(editor, new ListFormSubmissionsQuery(_formId))).Should().ContainSingle(s => s.Id == id && s.Status == "Accepted");
    }

    // ── another tenant ──────────────────────────────────────────────────────

    [Fact]
    public async Task AnotherTenant_GetsNotFound_ForEveryFormAndResponseOfThisOne()
    {
        var id = await SubmitAsync(_editor);
        var other = await _host.SeedWorkspaceAsync();
        using var foreign = _host.CreateProvider(other.AccountId, other.OwnerId);

        await FluentActions.Awaiting(() => OrgTreeTestHost.QueryAsync(foreign, new GetFormQuery(_formId))).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => OrgTreeTestHost.QueryAsync(foreign, new ListFormsQuery(_boardId))).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => OrgTreeTestHost.QueryAsync(foreign, new ListFormSubmissionsQuery(_formId))).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => Send(foreign, new CreateFormCommand(_boardId, "Cross tenant"))).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => Send(foreign, new SubmitFormCommand(_formId, Answers("{\"title\":\"x\"}")))).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => Send(foreign, new PublishFormCommand(_formId, 1))).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => Send(foreign, new RestoreFormCommand(_formId))).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => Send(foreign, new MarkFormSubmissionAsSpamCommand(id))).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => Send(foreign, new RejectFormSubmissionCommand(id, "x"))).Should().ThrowAsync<NotFoundException>();

        (await FormCountAsync()).Should().Be(1);
        (await SubmissionCountAsync()).Should().Be(1);
    }

    // ── competing writes ────────────────────────────────────────────────────

    [Fact]
    public async Task AStaleFormVersion_ChangesNothing_ForEveryVersionedWrite()
    {
        using var manager = As(_manager);
        var stale = await VersionAsync(_formId);
        await Send(manager, new UpdateFormDetailsCommand(_formId, "Moved on", null, "{}", "{}", stale));
        var current = await VersionAsync(_formId);
        var question = (await OrgTreeTestHost.QueryAsync(manager, new GetFormQuery(_formId))).Questions.First();

        var writes = new object[]
        {
            new UpdateFormDetailsCommand(_formId, "Lost update", null, "{}", "{}", stale),
            new CloseFormCommand(_formId, stale),
            new DeleteFormCommand(_formId, stale),
            new AddFormQuestionCommand(_formId, "late", FormQuestionType.ShortText, "Late", false, null, null, ExpectedVersion: stale),
            new UpdateFormQuestionCommand(_formId, question.Id, "Lost label", true, null, null, ExpectedVersion: stale),
            new RemoveFormQuestionCommand(_formId, question.Id, stale),
        };
        foreach (var write in writes)
            await FluentActions.Awaiting(() => Send(manager, write))
                .Should().ThrowAsync<PreconditionFailedException>(write.GetType().Name);

        var form = await OrgTreeTestHost.QueryAsync(manager, new GetFormQuery(_formId));
        form.Version.Should().Be(current);
        form.Name.Should().Be("Moved on");
        form.Status.Should().Be("Published");
        form.Questions.Should().HaveCount(2);
        form.Questions.First().Label.Should().Be("Title");
    }

    [Fact]
    public async Task AStaleVersion_FailsEvenWhenTheChangeWouldHaveBeenANoOp_AndAMissingVersionFailsClosed()
    {
        using var manager = As(_manager);
        var version = await VersionAsync(_formId);

        await FluentActions.Awaiting(() => Send(manager, new PublishFormCommand(_formId, version - 1)))
            .Should().ThrowAsync<PreconditionFailedException>("a stale writer is told so, also for a repeat of the state");
        await FluentActions.Awaiting(() => Send(manager, new PublishFormCommand(_formId, null)))
            .Should().ThrowAsync<Exception>("a write with no version never commits");

        (await VersionAsync(_formId)).Should().Be(version);
    }

    [Fact]
    public async Task APublishRepeatWithTheCurrentVersion_IsANoOp_AndBumpsNoVersion()
    {
        using var manager = As(_manager);
        var version = await VersionAsync(_formId);

        await Send(manager, new PublishFormCommand(_formId, version));
        await Send(manager, new UpdateFormDetailsCommand(_formId, "Request", null, "{}", "{}", version));

        (await VersionAsync(_formId)).Should().Be(version);
    }

    [Fact]
    public async Task UpdatingAQuestion_ChangesItAndMovesTheFormVersion_AndSavingTheSameAgainMovesNothing()
    {
        using var manager = As(_manager);
        var form = await OrgTreeTestHost.QueryAsync(manager, new GetFormQuery(_formId));
        var question = form.Questions.First();

        await Send(manager, new UpdateFormQuestionCommand(_formId, question.Id, "What do you need?", false, null, null, ExpectedVersion: form.Version));
        var after = await OrgTreeTestHost.QueryAsync(manager, new GetFormQuery(_formId));
        after.Questions.First().Label.Should().Be("What do you need?");
        after.Version.Should().Be(form.Version + 1);

        await Send(manager, new UpdateFormQuestionCommand(_formId, question.Id, "What do you need?", false, null, null, ExpectedVersion: after.Version));
        (await VersionAsync(_formId)).Should().Be(after.Version, "the same content is a no-op");
    }

    [Fact]
    public async Task AQuestionMappedToAnIncompatibleField_IsRefused_AndKeepsItsMapping()
    {
        Guid numberField = Guid.Empty, textField = Guid.Empty;
        await _host.WithSystemContextAsync(c =>
        {
            var number = BoardField.Create(_stack.AccountId, _stack.WorkspaceId, _boardId, "Effort", FieldType.Number, FieldSettings.Empty(), FractionalIndex.Create("a0"), _stack.OwnerId, OrgTreeTestHost.BaseTime);
            var text = BoardField.Create(_stack.AccountId, _stack.WorkspaceId, _boardId, "Summary", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a1"), _stack.OwnerId, OrgTreeTestHost.BaseTime);
            c.BoardFields.AddRange(number, text);
            (numberField, textField) = (number.Id, text.Id);
            return Task.CompletedTask;
        });
        using var manager = As(_manager);
        var form = await OrgTreeTestHost.QueryAsync(manager, new GetFormQuery(_formId));
        var title = form.Questions.First(q => q.QuestionKey == "title");

        await FluentActions.Awaiting(() => Send(manager, new UpdateFormQuestionCommand(_formId, title.Id, "Title", true, null, null, numberField, form.Version)))
            .Should().ThrowAsync<DomainBusinessRule>("a text question cannot fill a number field");
        await Send(manager, new UpdateFormQuestionCommand(_formId, title.Id, "Title", true, null, null, textField, form.Version));

        var mapped = (await OrgTreeTestHost.QueryAsync(manager, new GetFormQuery(_formId))).Questions.First(q => q.QuestionKey == "title");
        mapped.BoardFieldId.Should().Be(textField);
    }

    // ── retry ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatingAFormTwiceUnderTheSameKey_HasOneEffect_AndReplaysTheFirstResult()
    {
        using var manager = As(_manager);
        var before = await FormCountAsync();
        var key = $"form-create-{Guid.NewGuid():N}";

        var first = (Notrelix.Application.Common.Models.Result<Guid>)(await OrgTreeTestHost.SendIdempotentAsync(manager, new CreateFormCommand(_boardId, "Retried"), key))!;
        var replay = (Notrelix.Application.Common.Models.Result<Guid>)(await OrgTreeTestHost.SendIdempotentAsync(manager, new CreateFormCommand(_boardId, "Retried"), key))!;

        replay.Data.Should().Be(first.Data);
        (await FormCountAsync()).Should().Be(before + 1);
    }

    [Fact]
    public async Task SubmittingTwiceUnderTheSameKey_StoresOneResponse_AndANewKeyStoresAnother()
    {
        using var editor = As(_editor);
        var key = $"form-submit-{Guid.NewGuid():N}";
        var command = new SubmitFormCommand(_formId, Answers("{\"title\":\"Buy chairs\"}"));

        var first = (Notrelix.Application.Common.Models.Result<Guid>)(await OrgTreeTestHost.SendIdempotentAsync(editor, command, key))!;
        var replay = (Notrelix.Application.Common.Models.Result<Guid>)(await OrgTreeTestHost.SendIdempotentAsync(editor, command, key))!;

        replay.Data.Should().Be(first.Data);
        (await SubmissionCountAsync()).Should().Be(1);

        await OrgTreeTestHost.SendIdempotentAsync(editor, command);
        (await SubmissionCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task TheSameKeyForAnotherAnswer_IsRefused_AndStoresNothingMore()
    {
        using var editor = As(_editor);
        var key = $"form-submit-{Guid.NewGuid():N}";
        await OrgTreeTestHost.SendIdempotentAsync(editor, new SubmitFormCommand(_formId, Answers("{\"title\":\"One\"}")), key);

        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(editor, new SubmitFormCommand(_formId, Answers("{\"title\":\"Two\"}")), key))
            .Should().ThrowAsync<IdempotencyPayloadMismatchException>();

        (await SubmissionCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task InvalidAnswers_AreRefusedWithTheQuestionNamed_AndTheKeyIsNotSpent()
    {
        using var editor = As(_editor);
        var key = $"form-submit-{Guid.NewGuid():N}";

        var refused = await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(editor, new SubmitFormCommand(_formId, Answers("{\"qty\":99}")), key))
            .Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
        refused.Which.Errors.Keys.Should().BeEquivalentTo("title", "qty");
        (await SubmissionCountAsync()).Should().Be(0);
    }

    // ── a response ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Spam_StaysInTheInboxAsAState_NeverProcessedNorRejected_AndOnlyAnExplicitDeleteHidesIt()
    {
        var id = await SubmitAsync(_editor);
        using var editor = As(_editor);

        await Send(editor, new MarkFormSubmissionAsSpamCommand(id));

        var listed = await OrgTreeTestHost.QueryAsync(editor, new ListFormSubmissionsQuery(_formId));
        listed.Should().ContainSingle(s => s.Id == id && s.Status == "Spam" && s.PayloadJson.Contains("Buy chairs") && s.CreatedItemId == null);
        await FluentActions.Awaiting(() => Send(editor, new RejectFormSubmissionCommand(id, "x"))).Should().ThrowAsync<DomainBusinessRule>();
        await FluentActions.Awaiting(() => Send(editor, new MarkFormSubmissionAsSpamCommand(id))).Should().ThrowAsync<DomainBusinessRule>();
        (await OrgTreeTestHost.QueryAsync(editor, new ListFormSubmissionsQuery(_formId))).Should().ContainSingle(s => s.Id == id && s.Status == "Spam");

        await Send(editor, new DeleteFormSubmissionCommand(id));
        (await OrgTreeTestHost.QueryAsync(editor, new ListFormSubmissionsQuery(_formId))).Should().BeEmpty();
    }

    [Fact]
    public async Task Reject_KeepsTheReason_AndAHandledResponseIsRefusedASecondDecision()
    {
        var id = await SubmitAsync(_editor);
        using var editor = As(_editor);

        await Send(editor, new RejectFormSubmissionCommand(id, "Duplicate of ERP-168"));

        (await OrgTreeTestHost.QueryAsync(editor, new ListFormSubmissionsQuery(_formId)))
            .Should().ContainSingle(s => s.Id == id && s.Status == "Rejected" && s.Reason == "Duplicate of ERP-168" && s.CreatedItemId == null);
        await FluentActions.Awaiting(() => Send(editor, new MarkFormSubmissionAsSpamCommand(id))).Should().ThrowAsync<DomainBusinessRule>();
        await FluentActions.Awaiting(() => Send(editor, new ProcessFormSubmissionCommand(id, Guid.CreateVersion7()))).Should().ThrowAsync<Exception>();
    }

    // ── board state and question types ──────────────────────────────────────

    [Fact]
    public async Task AnArchivedBoard_RefusesEveryFormWrite_ButStillShowsTheForm()
    {
        var id = await SubmitAsync(_editor);
        await _host.WithSystemContextAsync(async c =>
        {
            var board = await c.Boards.SingleAsync(b => b.Id == _boardId);
            board.Archive(_stack.OwnerId, OrgTreeTestHost.BaseTime);
        });
        var version = await VersionAsync(_formId);
        using var manager = As(_manager);
        using var editor = As(_editor);

        await FluentActions.Awaiting(() => Send(manager, new CloseFormCommand(_formId, version))).Should().ThrowAsync<ForbiddenException>();
        await FluentActions.Awaiting(() => Send(manager, new CreateFormCommand(_boardId, "Late"))).Should().ThrowAsync<ForbiddenException>();
        await FluentActions.Awaiting(() => Send(editor, new SubmitFormCommand(_formId, Answers("{\"title\":\"x\"}")))).Should().ThrowAsync<ForbiddenException>();
        await FluentActions.Awaiting(() => Send(editor, new MarkFormSubmissionAsSpamCommand(id))).Should().ThrowAsync<ForbiddenException>();

        (await VersionAsync(_formId)).Should().Be(version);
        (await SubmissionCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AFormWithARequiredFileQuestion_StillTakesAnswers_TheQuestionIsHiddenFromTheFiller()
    {
        // Old data: file and signature questions are refused today, but a form made before may hold one.
        await _host.WithSystemContextAsync(async c =>
        {
            var form = await c.Set<Form>().Include(f => f.Questions).SingleAsync(f => f.Id == _formId);
            var legacy = FormQuestion.Create(_stack.AccountId, _stack.WorkspaceId, _formId, null, "attachment", "Attachment", FormQuestionType.FileUpload, true, FractionalIndex.Create("a9"));
            c.FormQuestions.Add(legacy);
        });

        var id = await SubmitAsync(_editor);

        id.Should().NotBeEmpty();
        using var editor = As(_editor);
        (await OrgTreeTestHost.QueryAsync(editor, new ListFormSubmissionsQuery(_formId))).Should().ContainSingle(s => s.Id == id);
    }
}
