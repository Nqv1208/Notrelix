using FluentAssertions;
using Notrelix.Domain.WorkManagement.Approvals;

namespace Notrelix.Domain.Tests.WorkManagement.Approvals;

public class ApprovalTeamDecisionTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private ApprovalRequest CreateRequestWithTeamStep(out Guid teamId)
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WorkspaceId);
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Test", Guid.NewGuid(), Now);
        teamId = Guid.NewGuid();
        request.AddStep(1, Guid.NewGuid(), Now, approverTeamId: teamId);
        return request;
    }

    [Fact]
    public void Approve_TeamStep_ByAMemberOfThatTeam_Succeeds_AndRecordsTheDecider()
    {
        var request = CreateRequestWithTeamStep(out var teamId);
        var step = request.Steps.Single();
        var member = Guid.NewGuid();
        request.Approve(step.Id, member, Now.AddMinutes(1), deciderTeamIds: [teamId]);
        step.Status.Should().Be(ApprovalStatus.Approved);
        step.DecidedByUserId.Should().Be(member);
    }

    [Fact]
    public void Reject_TeamStep_ByAMemberOfThatTeam_Succeeds()
    {
        var request = CreateRequestWithTeamStep(out var teamId);
        var step = request.Steps.Single();
        request.Reject(step.Id, Guid.NewGuid(), Now.AddMinutes(1), deciderTeamIds: [teamId]);
        step.Status.Should().Be(ApprovalStatus.Rejected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TeamStep_WithoutMembershipOfThatTeam_IsRefused_AndLeavesTheVersion(bool withOtherTeam)
    {
        var request = CreateRequestWithTeamStep(out _);
        var step = request.Steps.Single();
        var before = request.Version;
        IReadOnlyCollection<Guid>? teams = withOtherTeam ? [Guid.NewGuid()] : null;
        var act = () => request.Approve(step.Id, Guid.NewGuid(), Now.AddMinutes(1), deciderTeamIds: teams);
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be("WorkManagement_Approval_StepNotAssignedToYou");
        request.Version.Should().Be(before);
        step.DecidedByUserId.Should().BeNull();
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
