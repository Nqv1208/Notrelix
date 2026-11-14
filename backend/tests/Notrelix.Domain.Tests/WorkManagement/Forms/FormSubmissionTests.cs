using FluentAssertions;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Domain.Tests.WorkManagement;

public class FormSubmissionTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var submission = FormSubmission.Create(Guid.NewGuid(), workspaceId, formId, boardId, null, null, null, "{}", null, null, now);

        submission.WorkspaceId.Should().Be(workspaceId);
        submission.FormId.Should().Be(formId);
        submission.BoardId.Should().Be(boardId);
        submission.Status.Should().Be(FormSubmissionStatus.Accepted);
        submission.SubmittedAt.Should().Be(now);
        submission.DomainEvents.Should().ContainSingle(e => e is FormSubmissionCreatedDomainEvent);
    }

    [Fact]
    public void Create_ShouldDefaultPayloadToEmptyObject()
    {
        var submission = FormSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, null!, null, null, DateTimeOffset.UtcNow);

        submission.PayloadJson.Should().Be("{}");
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => FormSubmission.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null, null, null, "{}", null, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyFormId_ShouldThrow()
    {
        var act = () => FormSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null, null, null, "{}", null, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyBoardId_ShouldThrow()
    {
        var act = () => FormSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, null, null, "{}", null, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Reject_ShouldSetStatusAndRaiseEvent()
    {
        var submission = CreateSubmission();
        var now = DateTimeOffset.UtcNow;

        submission.Reject(now);

        submission.Status.Should().Be(FormSubmissionStatus.Rejected);
        submission.ProcessedAt.Should().Be(now);
        submission.DomainEvents.Should().ContainSingle(e => e is FormSubmissionRejectedDomainEvent);
    }

    [Fact]
    public void MarkAsSpam_ShouldSetStatusAndRaiseEvent()
    {
        var submission = CreateSubmission();
        var now = DateTimeOffset.UtcNow;

        submission.MarkAsSpam(now);

        submission.Status.Should().Be(FormSubmissionStatus.Spam);
        submission.ProcessedAt.Should().Be(now);
        submission.DomainEvents.Should().ContainSingle(e => e is FormSubmissionMarkedAsSpamDomainEvent);
    }

    [Fact]
    public void MarkProcessed_ShouldSetStatusAndRaiseEvent()
    {
        var submission = CreateSubmission();
        var createdItemId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        submission.MarkProcessed(createdItemId, now);

        submission.CreatedItemId.Should().Be(createdItemId);
        submission.ProcessedAt.Should().Be(now);
        submission.DomainEvents.Should().ContainSingle(e => e is FormSubmissionProcessedDomainEvent);
    }

    [Fact]
    public void Reject_MultipleCalls_ShouldThrow()
    {
        var submission = CreateSubmission();
        submission.Reject(DateTimeOffset.UtcNow);

        var later = DateTimeOffset.UtcNow.AddMinutes(5);
        var act = () => submission.Reject(later);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Delete_ShouldSetStatusAndRaiseEvent()
    {
        var submission = CreateSubmission();
        var deletedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        submission.Delete(deletedBy, now);

        submission.Status.Should().Be(FormSubmissionStatus.Deleted);
        submission.DomainEvents.Should().ContainSingle(e => e is FormSubmissionDeletedDomainEvent);
    }

    [Fact]
    public void Delete_MultipleCalls_ShouldThrow()
    {
        var submission = CreateSubmission();
        submission.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => submission.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkAsSpam_WhenRejected_ShouldThrow()
    {
        var submission = CreateSubmission();
        submission.Reject(DateTimeOffset.UtcNow);

        var act = () => submission.MarkAsSpam(DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkProcessed_WhenRejected_ShouldThrow()
    {
        var submission = CreateSubmission();
        submission.Reject(DateTimeOffset.UtcNow);

        var act = () => submission.MarkProcessed(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Reopen_ReturnsARejectedResponseToAccepted_AndClearsTheReason()
    {
        var submission = CreateSubmission();
        submission.Reject(DateTimeOffset.UtcNow, "Trùng");

        submission.Reopen();

        submission.Status.Should().Be(FormSubmissionStatus.Accepted);
        submission.RejectionReason.Should().BeNull();
        submission.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Reopen_ReturnsASpamOrDeletedResponseToAccepted()
    {
        var spam = CreateSubmission();
        spam.MarkAsSpam(DateTimeOffset.UtcNow);
        spam.Reopen();
        spam.Status.Should().Be(FormSubmissionStatus.Accepted);

        var deleted = CreateSubmission();
        deleted.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        deleted.Reopen();
        deleted.Status.Should().Be(FormSubmissionStatus.Accepted);
    }

    [Fact]
    public void Reopen_OfAnAcceptedResponse_IsANoOp()
    {
        var submission = CreateSubmission();
        var eventsBefore = submission.DomainEvents.Count;

        submission.Reopen();

        submission.Status.Should().Be(FormSubmissionStatus.Accepted);
        submission.DomainEvents.Should().HaveCount(eventsBefore, "a no-op raises nothing");
    }

    [Fact]
    public void Reopen_OfAProcessedResponse_KeepsItsItemAndProcessingTime()
    {
        var submission = CreateSubmission();
        var itemId = Guid.NewGuid();
        var processedAt = DateTimeOffset.UtcNow;
        submission.MarkProcessed(itemId, processedAt);
        submission.Reject(processedAt, "x");

        submission.Reopen();

        submission.Status.Should().Be(FormSubmissionStatus.Accepted);
        submission.CreatedItemId.Should().Be(itemId);
        submission.ProcessedAt.Should().Be(processedAt);
    }

    private static FormSubmission CreateSubmission()
    {
        return FormSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, "{}", null, null, DateTimeOffset.UtcNow);
    }
}
