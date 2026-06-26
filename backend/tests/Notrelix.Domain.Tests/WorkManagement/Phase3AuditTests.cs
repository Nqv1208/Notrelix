using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement;

public class Phase3AuditTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    #region Phase 3a — BoardItem.MoveToGroup validation

    [Fact]
    public void MoveToGroup_WithMatchingWorkspaceAndBoard_ShouldSucceed()
    {
        var groupId = Guid.NewGuid();
        var item = BoardItem.Create(WsA, BoardA, groupId, "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.ClearDomainEvents();

        var newGroup = Guid.NewGuid();
        var newPosition = FractionalIndex.Create("b0");
        var groupRef = new BoardGroupRef(WsA, BoardA, newGroup);

        item.MoveToGroup(groupRef, newPosition, Actor, Now);

        item.GroupId.Should().Be(newGroup);
        item.Position.Should().Be(newPosition);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemMovedDomainEvent);
        item.Version.Should().Be(2);
    }

    [Fact]
    public void MoveToGroup_WithMismatchedWorkspace_ShouldThrow()
    {
        var item = BoardItem.Create(WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);
        var groupRef = new BoardGroupRef(WsB, BoardA, Guid.NewGuid());

        var act = () => item.MoveToGroup(groupRef, FractionalIndex.Create("b0"), Actor, Now);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void MoveToGroup_WithMismatchedBoard_ShouldThrow()
    {
        var item = BoardItem.Create(WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);
        var groupRef = new BoardGroupRef(WsA, Guid.NewGuid(), Guid.NewGuid());

        var act = () => item.MoveToGroup(groupRef, FractionalIndex.Create("b0"), Actor, Now);
        act.Should().Throw<BoardMismatchException>();
    }

    [Fact]
    public void MoveToGroup_WithSameGroupAndPosition_ShouldNotIncrementVersion()
    {
        var groupId = Guid.NewGuid();
        var position = FractionalIndex.Create("a0");
        var item = BoardItem.Create(WsA, BoardA, groupId, "Item", position, Actor, Now);
        var version = item.Version;

        var groupRef = new BoardGroupRef(WsA, BoardA, groupId);
        item.MoveToGroup(groupRef, position, Actor, Now);

        item.Version.Should().Be(version);
    }

    #endregion

    #region Phase 3b — Atomic item key generation

    [Fact]
    public void GenerateNextItemIdentity_FirstItem_ShouldReturnSequenceOne()
    {
        var board = Board.Create(WsA, Actor, "Board", null, Now);
        board.ClearDomainEvents();

        var (sequence, key) = board.GenerateNextItemIdentity(Actor, Now);

        sequence.Should().Be(1);
        key.Should().Be("1");
        board.Version.Should().Be(2);
    }

    [Fact]
    public void GenerateNextItemIdentity_WithPrefix_ShouldApplyPrefix()
    {
        var board = Board.Create(WsA, Actor, "Board", null, Now, itemKeyPrefix: "TASK");
        board.ClearDomainEvents();

        var (sequence, key) = board.GenerateNextItemIdentity(Actor, Now);

        sequence.Should().Be(1);
        key.Should().Be("TASK-1");
    }

    [Fact]
    public void GenerateNextItemIdentity_ShouldIncrementSequentially()
    {
        var board = Board.Create(WsA, Actor, "Board", null, Now);
        board.ClearDomainEvents();

        var (seq1, _) = board.GenerateNextItemIdentity(Actor, Now);
        var (seq2, key2) = board.GenerateNextItemIdentity(Actor, Now);

        seq1.Should().Be(1);
        seq2.Should().Be(2);
        key2.Should().Be("2");
    }

    #endregion

    #region Phase 3b.2 — Item identity generation events

    [Fact]
    public void GenerateNextItemIdentity_ShouldEmitEvent()
    {
        var board = Board.Create(WsA, Actor, "Board", null, Now);
        board.ClearDomainEvents();

        var (sequence, key) = board.GenerateNextItemIdentity(Actor, Now);

        var ev = board.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<BoardItemIdentityGeneratedDomainEvent>().Subject;
        ev.BoardId.Should().Be(board.Id);
        ev.SequenceNumber.Should().Be(sequence);
        ev.ItemKey.Should().Be(key);
        ev.WorkspaceId.Should().Be(WsA);
        ev.UpdatedBy.Should().Be(Actor);
    }

    #endregion

    #region Phase 3c — Form events and validations

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

    #endregion
}
