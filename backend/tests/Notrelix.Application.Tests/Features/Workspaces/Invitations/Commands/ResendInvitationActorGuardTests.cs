using System.Reflection;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendInvitation;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Invitations;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

/// <summary>
/// WG-DEBT-002 security follow-up: resending re-issues the token that grants the invited role,
/// so a non-Owner actor (Admin) must not re-arm a legacy Admin invitation (created before Admin
/// stopped being invitable). Owner may; Member and Guest invitations stay resendable by an Admin.
/// A rejected resend leaves the token, generation and status untouched and publishes nothing.
/// </summary>
public class ResendInvitationActorGuardTests : WorkspaceHandlerTestBase
{
    private const string Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

    private readonly Mock<IOneTimeTokenService> _tokens = new();
    private readonly Mock<ISecretEncryptor> _encryptor = new();
    private readonly Mock<IIntegrationEventCollector> _events = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IGuestResourceInvitationFacts> _facts = new();

    public ResendInvitationActorGuardTests()
    {
        _tokens.Setup(t => t.Generate(TokenPurpose.WorkspaceInvitation))
            .Returns(new IssuedOneTimeToken("raw-token", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 1));
        _encryptor.Setup(e => e.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("protected");
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
    }

    private ResendInvitationCommandHandler Sut() => new(
        DbContextMock.Object, RequestContextMock.Object, _tokens.Object, _encryptor.Object,
        _events.Object, DateTimeProviderMock.Object, _actors.Object, _facts.Object);

    private WorkspaceInvitation Invitation(WorkspaceRole role)
    {
        // Admin is no longer invitable through Create; a legacy row is simulated by setting the role.
        var invitation = WorkspaceInvitation.Create(
            TestAccountId, TestWorkspaceId, "invitee@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(Hash), 1, TestUserId, TestNow.AddDays(-1));
        typeof(WorkspaceInvitation).GetProperty(nameof(WorkspaceInvitation.Role), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(invitation, role);
        return invitation;
    }

    [Fact]
    public async Task Admin_CannotResend_ALegacyAdminInvitation_AndNothingChanges()
    {
        var invitation = Invitation(WorkspaceRole.Admin);
        SetupInvitations(invitation);
        SetupMembers(CreateMember(WorkspaceRole.Admin));
        var tokenBefore = invitation.Token.Value;
        var generationBefore = invitation.TokenGeneration;

        Func<Task> act = () => Sut().Handle(new ResendInvitationCommand(TestWorkspaceId, invitation.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotAssignRole);
        invitation.Token.Value.Should().Be(tokenBefore);
        invitation.TokenGeneration.Should().Be(generationBefore);
        _tokens.Verify(t => t.Generate(It.IsAny<TokenPurpose>()), Times.Never);
        _events.Verify(e => e.Add(It.IsAny<IIntegrationEvent>()), Times.Never);
    }

    [Fact]
    public async Task Owner_CanResend_ALegacyAdminInvitation()
    {
        var invitation = Invitation(WorkspaceRole.Admin);
        SetupInvitations(invitation);
        SetupMembers(CreateMember(WorkspaceRole.Owner));

        var result = await Sut().Handle(new ResendInvitationCommand(TestWorkspaceId, invitation.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        invitation.TokenGeneration.Should().Be(2);
        _events.Verify(e => e.Add(It.IsAny<IIntegrationEvent>()), Times.Once);
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task Admin_CanStillResend_MemberAndGuestInvitations(WorkspaceRole invitedRole)
    {
        var invitation = Invitation(invitedRole);
        SetupInvitations(invitation);
        SetupMembers(CreateMember(WorkspaceRole.Admin));

        var result = await Sut().Handle(new ResendInvitationCommand(TestWorkspaceId, invitation.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        invitation.TokenGeneration.Should().Be(2);
    }

    [Fact]
    public async Task ActorWithoutActiveMembership_IsForbidden_AndNothingChanges()
    {
        var invitation = Invitation(WorkspaceRole.Member);
        SetupInvitations(invitation);
        SetupMembers();

        Func<Task> act = () => Sut().Handle(new ResendInvitationCommand(TestWorkspaceId, invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        invitation.TokenGeneration.Should().Be(1);
    }
}
