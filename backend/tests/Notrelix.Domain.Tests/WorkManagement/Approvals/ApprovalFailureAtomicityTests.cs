using FluentAssertions;
using Notrelix.Domain.WorkManagement.Approvals;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement.Approvals;

public class ApprovalFailureAtomicityTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private ApprovalRequest CreateRequest()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WorkspaceId);
        var userId = Guid.NewGuid();
        var request = ApprovalRequest.Create(AccountId, WorkspaceId, target, "Test", userId, Now);
        request.AddStep(1, userId, Now, approverUserId: userId);
        return request;
    }

    [Fact]
    public void Approve_InvalidStepId_ShouldNotMutateRoot()
    {
        var request = CreateRequest();
        var before = request.Version;
        var act = () => request.Approve(Guid.NewGuid(), Guid.NewGuid(), Now.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>();
        request.Version.Should().Be(before);
    }

    [Fact]
    public void Approve_DefaultDecisionTime_ShouldNotMutateRoot()
    {
        var request = CreateRequest();
        var step = request.Steps.Single();
        var before = request.Version;
        var act = () => request.Approve(step.Id, Guid.NewGuid(), default);
        act.Should().Throw<BusinessRuleException>();
        request.Version.Should().Be(before);
    }

    [Fact]
    public void Reject_InvalidStepId_ShouldNotMutateRoot()
    {
        var request = CreateRequest();
        var before = request.Version;
        var act = () => request.Reject(Guid.NewGuid(), Guid.NewGuid(), Now.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>();
        request.Version.Should().Be(before);
    }

    [Fact]
    public void Cancel_NonPending_ShouldNotMutateRoot()
    {
        var request = CreateRequest();
        var step = request.Steps.Single();
        var stepUser = step.ApproverUserId!.Value;
        request.Reject(step.Id, stepUser, Now.AddMinutes(1));
        var before = request.Version;
        var act = () => request.Cancel(Guid.NewGuid(), Now.AddMinutes(2));
        act.Should().Throw<BusinessRuleException>();
        request.Version.Should().Be(before);
    }
}
