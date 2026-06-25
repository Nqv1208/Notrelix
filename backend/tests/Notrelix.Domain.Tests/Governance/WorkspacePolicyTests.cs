using FluentAssertions;
using Notrelix.Domain.Governance.Policies;

namespace Notrelix.Domain.Tests.Governance;

public class WorkspacePolicyTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();

        var policy = WorkspacePolicy.Create(workspaceId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        policy.WorkspaceId.Should().Be(workspaceId);
        policy.GuestPolicy.Should().NotBeNull();
        policy.ResourcePolicy.Should().NotBeNull();
        policy.SharingPolicy.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => WorkspacePolicy.Create(Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void UpdatePolicy_ShouldReplacePoliciesAndRaiseEvent()
    {
        var policy = WorkspacePolicy.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        policy.ClearDomainEvents();

        var newGuestPolicy = GuestAccessPolicy.Create(false);
        var newResourcePolicy = ResourcePolicy.Create(true);
        var newSharingPolicy = SharingPolicy.Create(true, true);

        policy.UpdatePolicy(newGuestPolicy, newResourcePolicy, newSharingPolicy, Guid.NewGuid(), DateTimeOffset.UtcNow);

        policy.GuestPolicy.AllowGuestInvites.Should().BeFalse();
        policy.ResourcePolicy.AllowPublicSharing.Should().BeTrue();
        policy.SharingPolicy.AllowPublicSharing.Should().BeTrue();
        policy.SharingPolicy.AllowExternalInvite.Should().BeTrue();
        policy.DomainEvents.Should().ContainSingle(e => e is WorkspacePolicyUpdatedEvent);
    }

    [Fact]
    public void UpdatePolicy_WithNullGuestPolicy_ShouldKeepExisting()
    {
        var policy = WorkspacePolicy.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var originalGuest = policy.GuestPolicy;

        policy.UpdatePolicy(null, null, null, Guid.NewGuid(), DateTimeOffset.UtcNow);

        policy.GuestPolicy.Should().Be(originalGuest);
    }
}
