using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Services;

public class InvitationAcceptanceServiceTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IIdentityUserLookupService> _identityLookupMock = new();
    private readonly Mock<IAccountMembershipProvisioner> _accountProvisionerMock = new();
    private readonly Mock<IAccountStatusReader> _accountStatusReaderMock = new();

    private InvitationAcceptanceService CreateSut() => new(
        DbContextMock.Object,
        _identityLookupMock.Object,
        _accountProvisionerMock.Object,
        _accountStatusReaderMock.Object,
        DateTimeProviderMock.Object,
        GrantProjectionMock.Object);

    public InvitationAcceptanceServiceTests()
    {
        SetupDefaultUser();
        _accountStatusReaderMock.Setup(s => s.GetStatusAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountStatus.Active);
        _accountProvisionerMock.Setup(s => s.EnsureWorkspaceInviteeAccountMembershipAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupDefaultUser(UserStatus status = UserStatus.Active, bool emailConfirmed = true, string email = "test@test.com")
        => _identityLookupMock.Setup(s => s.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityUserSnapshot(TestUserId, email, emailConfirmed, status));

    private void SetupNoCurrentUser()
        => _identityLookupMock.Setup(s => s.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserSnapshot?)null);

    private WorkspaceInvitation CreateInvitation(string email = "test@test.com")
        => WorkspaceInvitation.Create(
            TestAccountId,
            TestWorkspaceId,
            email,
            WorkspaceRole.Member,
            InvitationTokenHash.Create("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"),
            1,
            TestUserId,
            TestNow);

    // ── Success: user is not a workspace member yet ─────────
    [Fact]
    public async Task Accept_WhenNotYetMember_ProvisionsAccountMembershipCreatesMemberAndSyncsGrant()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var members = new List<WorkspaceMember>();
        SetupMembers(members);
        var invitation = CreateInvitation();
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.WorkspaceSlug.Should().Be("test-workspace");
        result.Data.WorkspaceId.Should().Be(workspace.Id);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        members.Should().ContainSingle(m => m.UserId == TestUserId && m.Role == invitation.Role);
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            workspace.AccountId, TestUserId, invitation.InvitedBy, TestNow, It.IsAny<CancellationToken>()), Times.Once);
        GrantProjectionMock.Verify(p => p.SyncWorkspaceMemberGrantAsync(
            workspace.AccountId, invitation.WorkspaceId, TestUserId, invitation.Role, TestNow, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Success: user is already an active member ───────────
    [Fact]
    public async Task Accept_WhenAlreadyActiveMember_ConsumesInvitationWithoutNewMemberGrantOrRoleChange()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var existingMember = CreateMember(WorkspaceRole.Member);
        var members = new List<WorkspaceMember> { existingMember };
        SetupMembers(members);
        var invitation = CreateInvitation();
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        members.Should().ContainSingle(m => m.UserId == TestUserId);
        existingMember.Role.Should().Be(WorkspaceRole.Member);
        existingMember.Status.Should().Be(WorkspaceMemberStatus.Active);
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        GrantProjectionMock.Verify(p => p.SyncWorkspaceMemberGrantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Rejection: suspended member ─────────────────────────
    [Fact]
    public async Task Accept_WhenMemberSuspended_ReturnsFailureLeavingStateUnchanged()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var suspendedMember = CreateMember(WorkspaceRole.Member);
        suspendedMember.Suspend(TestUserId, TestNow, 2);
        var members = new List<WorkspaceMember> { suspendedMember };
        SetupMembers(members);
        var invitation = CreateInvitation();
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        members.Should().ContainSingle();
        suspendedMember.Status.Should().Be(WorkspaceMemberStatus.Suspended);
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        GrantProjectionMock.Verify(p => p.SyncWorkspaceMemberGrantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Rejection: removed member ───────────────────────────
    [Fact]
    public async Task Accept_WhenMemberRemoved_ReturnsFailureLeavingStateUnchanged()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var removedMember = CreateMember(WorkspaceRole.Member);
        removedMember.Remove(2, TestUserId, TestNow.AddDays(1));
        var members = new List<WorkspaceMember> { removedMember };
        SetupMembers(members);
        var invitation = CreateInvitation();
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        removedMember.Status.Should().Be(WorkspaceMemberStatus.Removed);
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        GrantProjectionMock.Verify(p => p.SyncWorkspaceMemberGrantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Rejection: email mismatch ───────────────────────────
    [Fact]
    public async Task Accept_WhenTargetEmailDoesNotMatchCurrentUser_ReturnsFailureSideEffectFree()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var members = new List<WorkspaceMember>();
        SetupMembers(members);
        var invitation = CreateInvitation(email: "someone-else@test.com");
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        members.Should().BeEmpty();
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        GrantProjectionMock.Verify(p => p.SyncWorkspaceMemberGrantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Rejection: expired invitation ───────────────────────
    [Fact]
    public async Task Accept_WhenInvitationExpired_ReturnsFailureSideEffectFree()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var members = new List<WorkspaceMember>();
        SetupMembers(members);
        var invitation = CreateInvitation();
        invitation.GetType().GetProperty(nameof(WorkspaceInvitation.ExpiresAt))!
            .SetValue(invitation, TestNow.AddDays(-1));
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        members.Should().BeEmpty();
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Rejection: invitation already consumed ──────────────
    [Fact]
    public async Task Accept_WhenInvitationNotPending_ReturnsFailureSideEffectFree()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var members = new List<WorkspaceMember>();
        SetupMembers(members);
        var invitation = CreateInvitation();
        invitation.GetType().GetProperty(nameof(WorkspaceInvitation.Status))!
            .SetValue(invitation, WorkspaceInvitationStatus.Declined);
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        members.Should().BeEmpty();
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        GrantProjectionMock.Verify(p => p.SyncWorkspaceMemberGrantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Rejection: workspace/account not active ─────────────
    [Fact]
    public async Task Accept_WhenWorkspaceArchived_ReturnsFailure()
    {
        var workspace = CreateWorkspace(TestWorkspaceId, isArchived: true);
        SetupWorkspaces(workspace);
        var members = new List<WorkspaceMember>();
        SetupMembers(members);
        var invitation = CreateInvitation();
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        members.Should().BeEmpty();
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Accept_WhenAccountNotActive_ReturnsFailure()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var members = new List<WorkspaceMember>();
        SetupMembers(members);
        _accountStatusReaderMock.Setup(s => s.GetStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountStatus.Suspended);
        var invitation = CreateInvitation();
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        members.Should().BeEmpty();
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Rejection: actor preconditions ──────────────────────
    [Fact]
    public async Task Accept_WhenCurrentUserNotFound_ReturnsFailure()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupNoCurrentUser();
        var members = new List<WorkspaceMember>();
        SetupMembers(members);
        var invitation = CreateInvitation();
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        members.Should().BeEmpty();
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Accept_WhenCurrentUserSuspendedOrEmailUnconfirmed_ReturnsFailure()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var members = new List<WorkspaceMember>();
        SetupMembers(members);
        var invitation = CreateInvitation();
        var sut = CreateSut();

        SetupDefaultUser(status: UserStatus.Suspended, emailConfirmed: true);
        (await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None)).Succeeded.Should().BeFalse();

        SetupDefaultUser(status: UserStatus.Active, emailConfirmed: false);
        (await sut.AcceptAsync(invitation, TestUserId, CancellationToken.None)).Succeeded.Should().BeFalse();

        members.Should().BeEmpty();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Accept_WhenActingUserEmpty_ReturnsFailure()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers();
        var invitation = CreateInvitation();
        var sut = CreateSut();

        var result = await sut.AcceptAsync(invitation, Guid.Empty, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        _accountProvisionerMock.Verify(p => p.EnsureWorkspaceInviteeAccountMembershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupMembers(List<WorkspaceMember> members)
        => DbContextMock.Setup(c => c.WorkspaceMembers).Returns(CreateMemberDbSet(members));

    private static DbSet<WorkspaceMember> CreateMemberDbSet(List<WorkspaceMember> members)
    {
        var mock = new Mock<DbSet<WorkspaceMember>>();
        var queryable = members.AsQueryable();

        mock.As<IAsyncEnumerable<WorkspaceMember>>()
            .Setup(s => s.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<WorkspaceMember>(members.GetEnumerator()));

        mock.As<IQueryable<WorkspaceMember>>()
            .Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<WorkspaceMember>(queryable));
        mock.As<IQueryable<WorkspaceMember>>()
            .Setup(m => m.Expression).Returns(queryable.Expression);
        mock.As<IQueryable<WorkspaceMember>>()
            .Setup(m => m.ElementType).Returns(queryable.ElementType);
        mock.As<IQueryable<WorkspaceMember>>()
            .Setup(m => m.GetEnumerator()).Returns(() => members.GetEnumerator());

        mock.Setup(m => m.Add(It.IsAny<WorkspaceMember>())).Callback<WorkspaceMember>(members.Add);

        return mock.Object;
    }
}