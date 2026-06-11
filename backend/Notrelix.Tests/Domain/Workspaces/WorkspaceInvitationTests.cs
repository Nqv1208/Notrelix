using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Xunit;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceInvitationTests
{
    [Fact]
    public void Accept_ShouldSucceed_WhenPendingAndNotExpired()
    {
        var workspaceId = Guid.NewGuid();
        var invitation = WorkspaceInvitation.Create(workspaceId, "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();

        invitation.Accept(userId, DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        invitation.DomainEvents.Should().ContainSingle(e => e is WorkspaceInvitationAcceptedEvent);
    }

    [Fact]
    public void Accept_ShouldThrow_WhenExpired()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow, TimeSpan.FromDays(-1));
        
        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [Fact]
    public void Accept_ShouldThrow_WithoutMutating_WhenExpired()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow, TimeSpan.FromDays(-1));

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        invitation.DomainEvents.Should().NotContain(e => e is WorkspaceInvitationExpiredEvent);
    }

    [Fact]
    public void Expire_ShouldSucceed_WhenPending()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        invitation.Expire(DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Expired);
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationExpiredEvent);
    }

    [Fact]
    public void Expire_ShouldDoNothing_WhenAlreadyExpired()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Expire(DateTimeOffset.UtcNow);
        invitation.ClearDomainEvents();

        invitation.Expire(DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Expired);
        invitation.DomainEvents.Should().NotContain(e => e is WorkspaceInvitationExpiredEvent);
    }

    [Fact]
    public void Accept_ShouldThrow_WhenDeleted()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Revoke_ShouldThrow_WhenDeleted()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }
}
