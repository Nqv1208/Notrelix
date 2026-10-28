using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RemoveFormQuestion;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Forms;
using AppValidation = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Application.Tests.Features.WorkManagement.Forms;

public class SubmitFormCommandTests : WorkManagementHandlerTestBase
{
    private readonly SubmitFormCommandHandler _handler;

    public SubmitFormCommandTests()
    {
        _handler = new SubmitFormCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
    }

    private static Dictionary<string, JsonElement> Answers(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private Form FormWithQuestions(bool publish = true, bool close = false)
    {
        var form = CreateForm();
        form.AddQuestion(Question(form, "title", FormQuestionType.ShortText, true), TestUserId, TestNow);
        form.AddQuestion(Question(form, "qty", FormQuestionType.Number, false, "{\"minValue\":1,\"maxValue\":10}"), TestUserId, TestNow);
        form.AddQuestion(Question(form, "agree", FormQuestionType.Checkbox, false), TestUserId, TestNow);
        if (publish) form.Publish(TestUserId, TestNow);
        if (close) form.Close(TestUserId, TestNow);
        return form;
    }

    private FormQuestion Question(Form form, string key, FormQuestionType type, bool required, string? config = null) =>
        FormQuestion.Create(TestAccountId, TestWorkspaceId, form.Id, null, key, key, type, required, FractionalIndex.Initial(), FormQuestionConfig.FromConfig(type, config));

    [Fact]
    public async Task Handle_ValidAnswers_RecordsAcceptedSubmission()
    {
        var form = FormWithQuestions();
        SetupForms(form);
        var added = new List<FormSubmission>();
        var set = CreateAsyncDbSet(added);
        DbContextMock.Setup(c => c.FormSubmissions).Returns(set);

        var result = await _handler.Handle(new SubmitFormCommand(form.Id, Answers("{\"title\":\"Mua ghế\",\"qty\":3}")), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        Mock.Get(set).Verify(s => s.Add(It.Is<FormSubmission>(x => x.FormId == form.Id && x.BoardId == form.BoardId && x.SubmitterUserId == TestUserId && x.Status == FormSubmissionStatus.Accepted)), Times.Once);
    }

    [Theory]
    [InlineData("{}", "title")]                                  // required missing
    [InlineData("{\"title\":\"  \"}", "title")]                  // required blank
    [InlineData("{\"title\":\"a\",\"qty\":99}", "qty")]          // above max
    [InlineData("{\"title\":\"a\",\"qty\":\"x\"}", "qty")]       // wrong shape
    [InlineData("{\"title\":\"a\",\"agree\":\"yes\"}", "agree")] // not a boolean
    [InlineData("{\"title\":\"a\",\"other\":1}", "other")]       // unknown question
    public async Task Handle_InvalidAnswers_ThrowsValidationNamingTheQuestion(string answers, string key)
    {
        var form = FormWithQuestions();
        SetupForms(form);

        var ex = (await _handler.Invoking(h => h.Handle(new SubmitFormCommand(form.Id, Answers(answers)), CancellationToken.None))
            .Should().ThrowAsync<AppValidation>()).Which;

        ex.Errors.Should().ContainKey(key);
    }

    [Fact]
    public async Task Handle_DraftForm_Refused()
    {
        var form = FormWithQuestions(publish: false);
        SetupForms(form);

        await _handler.Invoking(h => h.Handle(new SubmitFormCommand(form.Id, Answers("{\"title\":\"a\"}")), CancellationToken.None))
            .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_ClosedForm_Refused()
    {
        var form = FormWithQuestions(close: true);
        SetupForms(form);

        await _handler.Invoking(h => h.Handle(new SubmitFormCommand(form.Id, Answers("{\"title\":\"a\"}")), CancellationToken.None))
            .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_UnknownOrDeletedForm_ThrowsNotFound()
    {
        var form = FormWithQuestions();
        form.Delete(TestUserId, TestNow);
        SetupForms(form);

        await _handler.Invoking(h => h.Handle(new SubmitFormCommand(form.Id, Answers("{}")), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
        await _handler.Invoking(h => h.Handle(new SubmitFormCommand(Guid.CreateVersion7(), Answers("{}")), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    private Form FormWithChoice(FormQuestionType type, string? configJson)
    {
        var form = CreateForm();
        form.AddQuestion(Question(form, "pick", type, true, configJson), TestUserId, TestNow);
        form.Publish(TestUserId, TestNow);
        return form;
    }

    private const string Opts = "{\"options\":[{\"key\":\"low\",\"label\":\"Thấp\"},{\"key\":\"high\",\"label\":\"Cao\"}]}";

    [Theory]
    [InlineData(FormQuestionType.Select, "{\"pick\":\"high\"}", true)]
    [InlineData(FormQuestionType.Select, "{\"pick\":\"urgent\"}", false)]
    [InlineData(FormQuestionType.MultiSelect, "{\"pick\":[\"low\",\"high\"]}", true)]
    [InlineData(FormQuestionType.MultiSelect, "{\"pick\":[\"low\",\"nope\"]}", false)]
    public async Task Handle_UnmappedChoice_ChecksAnswerAgainstConfiguredOptions(FormQuestionType type, string answers, bool valid)
    {
        var form = FormWithChoice(type, Opts);
        SetupForms(form);
        DbContextMock.Setup(c => c.FormSubmissions).Returns(CreateAsyncDbSet(new List<FormSubmission>()));

        var act = () => _handler.Handle(new SubmitFormCommand(form.Id, Answers(answers)), CancellationToken.None);

        if (valid) (await act()).Succeeded.Should().BeTrue();
        else (await act.Should().ThrowAsync<AppValidation>()).Which.Errors.Should().ContainKey("pick");
    }

    [Fact]
    public async Task Handle_UnmappedChoiceWithoutOptions_RefusesEveryAnswer()
    {
        var form = FormWithChoice(FormQuestionType.Select, null);
        SetupForms(form);

        await _handler.Invoking(h => h.Handle(new SubmitFormCommand(form.Id, Answers("{\"pick\":\"x\"}")), CancellationToken.None))
            .Should().ThrowAsync<AppValidation>();
    }

    [Fact]
    public async Task RemoveQuestion_DropsItFromTheForm_AndRefusesAClosedForm()
    {
        var form = FormWithQuestions(publish: false);
        SetupForms(form);
        DbContextMock.Setup(c => c.FormQuestions).Returns(CreateAsyncDbSet(form.Questions.ToList()));
        var handler = new RemoveFormQuestionCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
        var qid = form.Questions.First().Id;

        (await handler.Handle(new RemoveFormQuestionCommand(form.Id, qid), CancellationToken.None)).Succeeded.Should().BeTrue();
        form.Questions.Should().NotContain(q => q.Id == qid);

        form.Close(TestUserId, TestNow);
        await handler.Invoking(h => h.Handle(new RemoveFormQuestionCommand(form.Id, form.Questions.First().Id), CancellationToken.None))
            .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
    }
}
