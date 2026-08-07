using FluentAssertions;
using Notrelix.Domain.WorkManagement.Approvals;

namespace Notrelix.Domain.Tests.WorkManagement.Approvals;

public class ApprovalTeamDecisionUnsupportedTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private ApprovalRequest CreateRequestWithTeamStep()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Test", Guid.NewGuid(), Now);
        var teamId = Guid.NewGuid();
        request.AddStep(1, Guid.NewGuid(), Now, approverTeamId: teamId);
        return request;
    }

    [Fact]
    public void Approve_TeamStep_ShouldThrow()
    {
        var request = CreateRequestWithTeamStep();
        var step = request.Steps.Single();
        var act = () => request.Approve(step.Id, Guid.NewGuid(), Now.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be("WorkManagement_Approval_TeamDecisionNotSupported");
    }

    [Fact]
    public void Reject_TeamStep_ShouldThrow()
    {
        var request = CreateRequestWithTeamStep();
        var step = request.Steps.Single();
        var act = () => request.Reject(step.Id, Guid.NewGuid(), Now.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be("WorkManagement_Approval_TeamDecisionNotSupported");
    }

    [Fact]
    public void Approve_TeamStep_ShouldNotMutateRootVersion()
    {
        var request = CreateRequestWithTeamStep();
        var step = request.Steps.Single();
        var before = request.Version;
        var act = () => request.Approve(step.Id, Guid.NewGuid(), Now.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>();
        request.Version.Should().Be(before);
    }

    [Fact]
    public void Approve_UserAssignedStep_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Test", Guid.NewGuid(), Now);
        var userId = Guid.NewGuid();
        request.AddStep(1, Guid.NewGuid(), Now, approverUserId: userId);
        var step = request.Steps.Single();
        request.Approve(step.Id, userId, Now.AddMinutes(1));
        step.Status.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public void Reject_UserAssignedStep_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Test", Guid.NewGuid(), Now);
        var userId = Guid.NewGuid();
        request.AddStep(1, Guid.NewGuid(), Now, approverUserId: userId);
        var step = request.Steps.Single();
        request.Reject(step.Id, userId, Now.AddMinutes(1));
        step.Status.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public void Approve_OtherUserStep_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Test", Guid.NewGuid(), Now);
        var userId = Guid.NewGuid();
        request.AddStep(1, Guid.NewGuid(), Now, approverUserId: userId);
        var step = request.Steps.Single();
        var act = () => request.Approve(step.Id, Guid.NewGuid(), Now.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be("WorkManagement_Approval_StepNotAssignedToYou");
    }
}
