using FluentAssertions;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Domain.Tests.WorkManagement.Forms;

public class FormLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Form_Publish_ShouldEmitEventAndUpdateStatus()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.AddQuestion(FormQuestion.Create(WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0")), Actor, Now);
        form.ClearDomainEvents();

        form.Publish(Actor, Now);

        form.Status.Should().Be(FormStatus.Published);
        form.DomainEvents.Should().ContainSingle(e => e is FormPublishedDomainEvent);
        form.Version.Should().Be(3);
    }

    [Fact]
    public void Form_Publish_WithNoQuestions_ShouldThrow()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);

        var act = () => form.Publish(Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*no questions*");
    }

    [Fact]
    public void Form_Publish_WhenClosed_ShouldThrow()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.AddQuestion(FormQuestion.Create(WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0")), Actor, Now);
        form.Close(Actor, Now);

        var act = () => form.Publish(Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*closed*");
    }

    [Fact]
    public void Form_Close_ShouldEmitEventAndUpdateStatus()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.ClearDomainEvents();

        form.Close(Actor, Now);

        form.Status.Should().Be(FormStatus.Closed);
        form.DomainEvents.Should().ContainSingle(e => e is FormClosedDomainEvent);
        form.Version.Should().Be(2);
    }

    [Fact]
    public void Form_Close_WhenAlreadyClosed_ShouldNotIncrementVersion()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.Close(Actor, Now);
        var version = form.Version;

        form.Close(Actor, Now);

        form.Version.Should().Be(version);
    }

    [Fact]
    public void Form_AddQuestion_ShouldEmitEvent()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.ClearDomainEvents();

        var question = FormQuestion.Create(WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0"));
        form.AddQuestion(question, Actor, Now);

        form.Questions.Should().HaveCount(1);
        form.DomainEvents.Should().ContainSingle(e => e is FormQuestionAddedDomainEvent);
    }

    [Fact]
    public void Form_AddQuestion_WhenClosed_ShouldThrow()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.Close(Actor, Now);

        var question = FormQuestion.Create(WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0"));
        var act = () => form.AddQuestion(question, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*closed*");
    }

    [Fact]
    public void Form_EnsureAcceptsSubmissions_WhenDraft_ShouldThrow()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);

        var act = () => form.EnsureAcceptsSubmissions();
        act.Should().Throw<BusinessRuleException>().WithMessage("*draft*");
    }

    [Fact]
    public void Form_EnsureAcceptsSubmissions_WhenClosed_ShouldThrow()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.Close(Actor, Now);

        var act = () => form.EnsureAcceptsSubmissions();
        act.Should().Throw<BusinessRuleException>().WithMessage("*closed*");
    }

    [Fact]
    public void Form_EnsureAcceptsSubmissions_WhenPublished_ShouldSucceed()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.AddQuestion(FormQuestion.Create(WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0")), Actor, Now);
        form.Publish(Actor, Now);

        var act = () => form.EnsureAcceptsSubmissions();
        act.Should().NotThrow();
    }
}
