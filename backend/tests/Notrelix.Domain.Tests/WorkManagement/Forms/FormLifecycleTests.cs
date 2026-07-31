using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Domain.Tests.WorkManagement.Forms;

[CoversAggregate(typeof(Form))]
public class FormLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(Form), nameof(Form.Publish), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Form), nameof(Form.UpdateDetails), MutationScenario.Event, typeof(string), typeof(BoardVisibility), typeof(string), typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Form_Publish_ShouldEmitEventAndUpdateStatus()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.AddQuestion(FormQuestion.Create(Guid.NewGuid(), WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0")), Actor, Now);
        ((IHasDomainEvents)form).ClearDomainEvents();

        form.Publish(Actor, Now);

        form.Status.Should().Be(FormStatus.Published);
        form.DomainEvents.Should().ContainSingle(e => e is FormPublishedDomainEvent);
        form.Version.Should().Be(3);
    }

    [CoversMutation(typeof(Form), nameof(Form.Publish), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Form_Publish_WithNoQuestions_ShouldThrow()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);

        var act = () => form.Publish(Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*no questions*");
    }

    [CoversMutation(typeof(Form), nameof(Form.Publish), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Form_Publish_WhenClosed_ShouldThrow()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.AddQuestion(FormQuestion.Create(Guid.NewGuid(), WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0")), Actor, Now);
        form.Close(Actor, Now);

        var act = () => form.Publish(Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*closed*");
    }

    [CoversMutation(typeof(Form), nameof(Form.Close), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Form), nameof(Form.UpdateDetails), MutationScenario.Event, typeof(string), typeof(BoardVisibility), typeof(string), typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Form_Close_ShouldEmitEventAndUpdateStatus()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        ((IHasDomainEvents)form).ClearDomainEvents();

        form.Close(Actor, Now);

        form.Status.Should().Be(FormStatus.Closed);
        form.DomainEvents.Should().ContainSingle(e => e is FormClosedDomainEvent);
        form.Version.Should().Be(2);
    }

    [CoversMutation(typeof(Form), nameof(Form.Close), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Form_Close_WhenAlreadyClosed_ShouldNotIncrementVersion()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.Close(Actor, Now);
        var version = form.Version;

        form.Close(Actor, Now);

        form.Version.Should().Be(version);
    }

    [CoversMutation(typeof(Form), nameof(Form.AddQuestion), MutationScenario.Event, typeof(FormQuestion), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Form_AddQuestion_ShouldEmitEvent()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        ((IHasDomainEvents)form).ClearDomainEvents();

        var question = FormQuestion.Create(Guid.NewGuid(), WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0"));
        form.AddQuestion(question, Actor, Now);

        form.Questions.Should().HaveCount(1);
        form.DomainEvents.Should().ContainSingle(e => e is FormQuestionAddedDomainEvent);
    }

    [CoversMutation(typeof(Form), nameof(Form.Close), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Form_AddQuestion_WhenClosed_ShouldThrow()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.Close(Actor, Now);

        var question = FormQuestion.Create(Guid.NewGuid(), WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0"));
        var act = () => form.AddQuestion(question, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*closed*");
    }

    [Fact]
    public void Form_EnsureAcceptsSubmissions_WhenDraft_ShouldThrow()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);

        var act = () => form.EnsureAcceptsSubmissions();
        act.Should().Throw<BusinessRuleException>().WithMessage("*draft*");
    }

    [Fact]
    public void Form_EnsureAcceptsSubmissions_WhenClosed_ShouldThrow()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.Close(Actor, Now);

        var act = () => form.EnsureAcceptsSubmissions();
        act.Should().Throw<BusinessRuleException>().WithMessage("*closed*");
    }

    [Fact]
    public void Form_EnsureAcceptsSubmissions_WhenPublished_ShouldSucceed()
    {
        var form = Form.Create(Guid.NewGuid(), WsA, BoardA, "Form", "form", Actor, Now);
        form.AddQuestion(FormQuestion.Create(Guid.NewGuid(), WsA, form.Id, null, "q1", "Q1", FormQuestionType.ShortText, true, FractionalIndex.Create("a0")), Actor, Now);
        form.Publish(Actor, Now);

        var act = () => form.EnsureAcceptsSubmissions();
        act.Should().NotThrow();
    }
}
