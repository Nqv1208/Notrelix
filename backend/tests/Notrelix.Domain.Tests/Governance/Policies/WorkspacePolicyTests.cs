using FluentAssertions;
using Notrelix.Domain.Governance.Policies;

namespace Notrelix.Domain.Tests.Governance;

public class WorkspacePolicyTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSucceed()
    {
        var policy = WorkspacePolicy.Create(AccountId, WorkspaceId, Actor, Now);

        policy.WorkspaceId.Should().Be(WorkspaceId);
        policy.GuestPolicy.Should().NotBeNull();
        policy.ResourcePolicy.Should().NotBeNull();
        policy.SharingPolicy.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => WorkspacePolicy.Create(AccountId, Guid.Empty, Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void UpdatePolicy_ShouldReplacePoliciesAndRaiseEvent()
    {
        var policy = WorkspacePolicy.Create(AccountId, WorkspaceId, Actor, Now);
        ((IHasDomainEvents)policy).ClearDomainEvents();

        var newGuestPolicy = GuestAccessPolicy.Create(false);
        var newResourcePolicy = ResourcePolicy.Create(true);
        var newSharingPolicy = SharingPolicy.Create(true, true);

        policy.UpdatePolicy(newGuestPolicy, newResourcePolicy, newSharingPolicy, Actor, Now);

        policy.GuestPolicy.AllowGuestInvites.Should().BeFalse();
        policy.ResourcePolicy.AllowPublicSharing.Should().BeTrue();
        policy.SharingPolicy.AllowPublicSharing.Should().BeTrue();
        policy.SharingPolicy.AllowExternalInvite.Should().BeTrue();
        policy.DomainEvents.Should().ContainSingle(e => e is WorkspacePolicyUpdatedDomainEvent);
    }

    [Fact]
    public void UpdatePolicy_WithNullGuestPolicy_ShouldKeepExisting()
    {
        var policy = WorkspacePolicy.Create(AccountId, WorkspaceId, Actor, Now);
        var originalGuest = policy.GuestPolicy;

        policy.UpdatePolicy(null, null, null, Actor, Now);

        policy.GuestPolicy.Should().Be(originalGuest);
    }
}
