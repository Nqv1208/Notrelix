using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.Approvals;

namespace Notrelix.Domain.Tests.WorkManagement.Approvals;

[CoversAggregate(typeof(ApprovalRequest))]
public class ApprovalMutationContractTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void AddStep_ShouldAlwaysUpdateAuditAndVersion()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        ((IHasDomainEvents)request).ClearDomainEvents();
        var version = request.Version;

        request.AddStep(1, Actor, Now, approverUserId: Guid.NewGuid());

        request.Version.Should().Be(version + 1);
        request.Steps.Should().HaveCount(1);
    }

    [Fact]
    public void AddStep_ShouldNotRaiseEvent_WhenNoStepAdded()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        request.AddStep(1, Actor, Now, approverUserId: Guid.NewGuid());
        ((IHasDomainEvents)request).ClearDomainEvents();
        var version = request.Version;

        Action act = () => request.AddStep(1, Actor, Now, approverUserId: Guid.NewGuid());

        act.Should().Throw<BusinessRuleException>().WithMessage("*position*");
        request.Version.Should().Be(version);
    }

    [Fact]
    public void Approve_ShouldThrow_WhenStepNotAssignedToUser()
    {
        var approverId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        request.AddStep(1, Actor, Now, approverUserId: approverId);

        var stepId = request.Steps.First().Id;
        Action act = () => request.Approve(stepId, otherUserId, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*not assigned to you*");
    }

    [Fact]
    public void Approve_ShouldSucceed_WhenCorrectApprover()
    {
        var approverId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        request.AddStep(1, Actor, Now, approverUserId: approverId);

        var stepId = request.Steps.First().Id;
        request.Approve(stepId, approverId, Now);

        request.Status.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public void Reject_ShouldThrow_WhenStepNotAssignedToUser()
    {
        var approverId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        request.AddStep(1, Actor, Now, approverUserId: approverId);

        var stepId = request.Steps.First().Id;
        Action act = () => request.Reject(stepId, otherUserId, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*not assigned to you*");
    }

    [Fact]
    public void Reject_ShouldSucceed_WhenCorrectApprover()
    {
        var approverId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        request.AddStep(1, Actor, Now, approverUserId: approverId);

        var stepId = request.Steps.First().Id;
        request.Reject(stepId, approverId, Now);

        request.Status.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public void Approve_ShouldThrow_WhenTeamStepAndDecidedByIsUserId()
    {
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        request.AddStep(1, Actor, Now, approverTeamId: teamId);

        var stepId = request.Steps.First().Id;
        Action act = () => request.Approve(stepId, userId, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*Team membership resolution*");
    }

    [Fact]
    public void Reject_ShouldThrow_WhenTeamStepAndDecidedByIsUserId()
    {
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        request.AddStep(1, Actor, Now, approverTeamId: teamId);

        var stepId = request.Steps.First().Id;
        Action act = () => request.Reject(stepId, userId, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*Team membership resolution*");
    }

    [Fact]
    public void Approve_ShouldThrow_WhenStepAlreadyApproved()
    {
        var approverId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        request.AddStep(1, Actor, Now, approverUserId: approverId);

        var stepId = request.Steps.First().Id;
        request.Approve(stepId, approverId, Now);

        Action act = () => request.Approve(stepId, approverId, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*pending*");
    }

    [Fact]
    public void Approve_ShouldThrow_WhenRequestAlreadyRejected()
    {
        var approverId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Approve", Actor, Now);
        request.AddStep(1, Actor, Now, approverUserId: approverId);

        var stepId = request.Steps.First().Id;
        request.Reject(stepId, approverId, Now);

        Action act = () => request.Approve(stepId, approverId, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*pending*");
    }
}
