using System.Text.Json;
using Notrelix.Application.Common.Data.Rls;
using Notrelix.Application.Features.Accounts.Public.Status;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitPublicForm;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetPublicForm;
using Notrelix.Application.Features.WorkManagement.Forms.Services;
using Notrelix.Application.Features.Workspaces.Public.Sharing;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Application.Tests.Features.WorkManagement.Forms;

/// <summary>
/// The anonymous submit of a public form (the per-form principal). A form is open to the public only while
/// it is published, public (it has a key), not deleted, and its workspace is active with public links on;
/// everything else is a 404 that does not say which. A missing or refused captcha stores nothing.
/// </summary>
public class PublicFormSubmitTests : WorkManagementHandlerTestBase
{
    private const string Key = "pub-key-abcdefghijklmnop";

    private readonly Mock<ICaptchaVerifier> _captcha = new();
    private readonly Mock<IRlsSessionContext> _rls = new();
    private readonly Mock<ICurrentTenantContext> _tenant = new();
    private readonly Mock<IWorkspaceSharingFacts> _sharing = new();
    private readonly Mock<IAccountStatusFacts> _account = new();
    private readonly List<FormSubmission> _added = [];

    public PublicFormSubmitTests()
    {
        DateTimeProviderMock.Setup(d => d.UtcNow).Returns(TestNow);
        _captcha.Setup(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? token, string? _, CancellationToken _) => token == "good");
        _captcha.SetupGet(c => c.SiteKey).Returns("site-key");
        _account.Setup(a => a.GetAsync(TestAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(AccountStatusFact.Active);
        _sharing.Setup(s => s.FindAsync(TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceSharingFacts(IsActive: true, AllowsPublicLinks: true));
        DbContextMock.Setup(c => c.FormSubmissions).Returns(CreateAsyncDbSet(new List<FormSubmission>()));
        DbContextMock.Setup(c => c.BoardFields).Returns(CreateAsyncDbSet(new List<Notrelix.Domain.WorkManagement.Fields.BoardField>()));
    }

    private PublicFormScope Scope() => new(DbContextMock.Object, _tenant.Object, _rls.Object, _sharing.Object, _account.Object);

    private SubmitPublicFormCommandHandler Handler() => new(Scope(), _captcha.Object, DbContextMock.Object, DateTimeProviderMock.Object);

    private Form OpenForm(FormStatus status = FormStatus.Published, bool isPublic = true)
    {
        var form = CreateForm();
        form.AddQuestion(FormQuestion.Create(TestAccountId, TestWorkspaceId, form.Id, null, "name", "Name", FormQuestionType.ShortText, true, FractionalIndex.Create("a0")), TestUserId, TestNow);
        if (status == FormStatus.Published) form.Publish(TestUserId, TestNow);
        if (status == FormStatus.Closed) form.Close(TestUserId, TestNow);
        if (isPublic) form.MakePublic(Key, TestUserId, TestNow);
        SetupForms(form);
        DbContextMock.Setup(c => c.FormSubmissions.Add(It.IsAny<FormSubmission>()))
            .Callback<FormSubmission>(s => _added.Add(s));
        return form;
    }

    private static SubmitPublicFormCommand Submit(string? captcha = "good", string? website = null, string? key = null,
        Dictionary<string, JsonElement>? answers = null) =>
        new(key ?? Key, answers ?? new() { ["name"] = JsonSerializer.SerializeToElement("Ada") }, captcha, website, "203.0.113.9");

    [Fact]
    public async Task Submit_ToAnOpenPublicForm_StoresAnAcceptedAnonymousSubmission()
    {
        var form = OpenForm();

        var result = await Handler().Handle(Submit(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var stored = _added.Should().ContainSingle().Subject;
        stored.FormId.Should().Be(form.Id);
        stored.BoardId.Should().Be(form.BoardId);
        stored.SubmitterUserId.Should().BeNull("the principal is the form's, never a person");
        stored.CreatedItemId.Should().BeNull("an anonymous visitor never creates an item directly; the inbox does");
        stored.Status.Should().Be(FormSubmissionStatus.Accepted);
    }

    [Fact]
    public async Task Submit_PrivateForm_IsNotFound_AndStoresNothing()
    {
        OpenForm(isPublic: false);

        var act = () => Handler().Handle(Submit(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _added.Should().BeEmpty();
        _captcha.Verify(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never,
            "an unknown form must not spend a captcha check or reveal that it exists");
    }

    [Fact]
    public async Task Submit_UnknownKey_IsNotFound()
    {
        OpenForm();

        var act = () => Handler().Handle(Submit(key: "some-other-key-abcdefghij"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(FormStatus.Draft)]
    [InlineData(FormStatus.Closed)]
    public async Task Submit_DraftOrClosedForm_IsNotFound(FormStatus status)
    {
        OpenForm(status);

        var act = () => Handler().Handle(Submit(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_ToADeletedForm_IsNotFound()
    {
        var form = OpenForm();
        form.Delete(TestUserId, TestNow);

        var act = () => Handler().Handle(Submit(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Submit_ToAnArchivedWorkspace_IsRefused_AsNotFound()
    {
        OpenForm();
        _sharing.Setup(s => s.FindAsync(TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceSharingFacts(IsActive: false, AllowsPublicLinks: true));

        var act = () => Handler().Handle(Submit(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_WhenTheWorkspaceTurnedPublicLinksOff_IsRefused_AsNotFound()
    {
        OpenForm();
        _sharing.Setup(s => s.FindAsync(TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceSharingFacts(IsActive: true, AllowsPublicLinks: false));

        var act = () => Handler().Handle(Submit(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(AccountStatusFact.Suspended)]
    [InlineData(AccountStatusFact.Closed)]
    public async Task ASuspendedOrClosedAccount_RefusesTheAnonymousReadAndSubmit_AsNotFound_AndStoresNothing(AccountStatusFact status)
    {
        OpenForm();
        _account.Setup(a => a.GetAsync(TestAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var submit = () => Handler().Handle(Submit(), CancellationToken.None);
        var read = () => new GetPublicFormQueryHandler(Scope(), _captcha.Object).Handle(new GetPublicFormQuery(Key), CancellationToken.None);

        await submit.Should().ThrowAsync<NotFoundException>();
        await read.Should().ThrowAsync<NotFoundException>();
        _added.Should().BeEmpty();
        _captcha.Verify(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never,
            "a suspended Account spends no captcha check and reveals nothing");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("forged")]
    public async Task Submit_WithAMissingOrInvalidCaptcha_IsRefused_AndStoresNothing(string? token)
    {
        OpenForm();

        var act = () => Handler().Handle(Submit(captcha: token), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>())
            .Which.Errors.Should().ContainKey("captchaToken");
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_PassesTheCallerAddressToTheCaptchaProvider()
    {
        OpenForm();

        await Handler().Handle(Submit(), CancellationToken.None);

        _captcha.Verify(c => c.VerifyAsync("good", "203.0.113.9", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Submit_WithAFilledHoneypot_AnswersSuccess_AndStoresNothing()
    {
        OpenForm();

        var result = await Handler().Handle(Submit(website: "http://bot.example"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_WithAnInvalidAnswer_IsRefused()
    {
        OpenForm();

        var act = () => Handler().Handle(Submit(answers: new() { ["name"] = JsonSerializer.SerializeToElement("") }), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_UnderTheDatabaseScope_EntersTheFormScopeFirst_AndAnUnknownKeyEntersNothing()
    {
        OpenForm();
        _rls.SetupGet(r => r.IsSessionContextEnabled).Returns(true);
        _rls.Setup(r => r.EnterFormSubmitScopeAsync(Key, It.IsAny<SystemOperationReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FormSubmitBinding?)null);

        var act = () => Handler().Handle(Submit(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("the database refused the scope, so the form is not open to the public");
        _tenant.Verify(t => t.SetWorkspace(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>()), Times.Never);
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_UnderTheDatabaseScope_AdoptsOnlyTheBoundTenant_WithNoUser()
    {
        var form = OpenForm();
        _rls.SetupGet(r => r.IsSessionContextEnabled).Returns(true);
        _rls.Setup(r => r.EnterFormSubmitScopeAsync(Key, It.IsAny<SystemOperationReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FormSubmitBinding(form.Id, TestAccountId, TestWorkspaceId, form.BoardId));
        _rls.Setup(r => r.ReadFormSubmitFieldOptionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await Handler().Handle(Submit(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _tenant.Verify(t => t.SetWorkspace(TestAccountId, TestWorkspaceId, null), Times.Once);
        _sharing.Verify(s => s.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never,
            "under the database scope the gate is the database's; the principal cannot read the workspace");
    }

    // ── the read side the anonymous page renders ──────────────

    [Fact]
    public async Task GetPublicForm_ReturnsOnlyWhatTheVisitorFills_AndTheCaptchaSiteKey()
    {
        OpenForm();
        var handler = new GetPublicFormQueryHandler(Scope(), _captcha.Object);

        var result = await handler.Handle(new GetPublicFormQuery(Key), CancellationToken.None);

        result.Data!.Name.Should().Be("Test Form");
        result.Data.CaptchaSiteKey.Should().Be("site-key");
        result.Data.Questions.Should().ContainSingle(q => q.Key == "name" && q.Required);
        typeof(PublicFormQuestionDto).GetProperties().Select(p => p.Name)
            .Should().NotContain(["BoardFieldId", "Id"], "an anonymous page never learns board or field ids");
    }

    [Fact]
    public async Task GetPublicForm_PrivateForm_IsNotFound()
    {
        OpenForm(isPublic: false);
        var handler = new GetPublicFormQueryHandler(Scope(), _captcha.Object);

        var act = () => handler.Handle(new GetPublicFormQuery(Key), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void TheAnonymousRequests_AreAnonymous_NeverAuthenticatedAndNeverCarryAPermission()
    {
        foreach (var type in new[] { typeof(SubmitPublicFormCommand), typeof(GetPublicFormQuery) })
        {
            typeof(IAnonymousRequest).IsAssignableFrom(type).Should().BeTrue(type.Name);
            typeof(IAuthenticatedRequest).IsAssignableFrom(type).Should().BeFalse(type.Name);
            typeof(IRequirePermission).IsAssignableFrom(type).Should().BeFalse(type.Name);
        }
    }
}
