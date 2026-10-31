using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

/// <summary>Inviting a person who is already an Active member is a conflict; someone who left may be invited again.</summary>
public class InviteMemberActiveMemberTests : WorkspaceHandlerTestBase
{
    private const string Email = "linh@example.com";

    private readonly Mock<IOneTimeTokenService> _tokens = new();
    private readonly Mock<ISecretEncryptor> _encryptor = new();
    private readonly Mock<IIntegrationEventCollector> _events = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IMemberEmailDisclosure> _memberEmails = new();
    private readonly List<IIntegrationEvent> _published = [];
    private readonly Guid _linhId = Guid.CreateVersion7();

    public InviteMemberActiveMemberTests()
    {
        _events.Setup(e => e.Add(It.IsAny<IIntegrationEvent>())).Callback<IIntegrationEvent>(_published.Add);
        _tokens.Setup(t => t.Generate(TokenPurpose.WorkspaceInvitation))
            .Returns(new IssuedOneTimeToken("raw-token", new string('a', 64), 1));
        _encryptor.Setup(e => e.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("protected");
        // Only the people the handler asks about are answered, like the disclosure rule.
        _memberEmails.Setup(m => m.ResolveAsync(TestWorkspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                ids.Contains(_linhId) ? new Dictionary<Guid, string> { [_linhId] = Email } : new Dictionary<Guid, string>());
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
    }

    private InviteMemberCommandHandler Handler() => new(
        DbContextMock.Object, _actors.Object, RequestContextMock.Object, DateTimeProviderMock.Object,
        _tokens.Object, _encryptor.Object, _events.Object,
        new InvitationInitialEntries(DbContextMock.Object, new Mock<Notrelix.Application.Features.Workspaces.Spaces.Services.ISpaceStore>().Object),
        _memberEmails.Object);

    private Task<Notrelix.Application.Common.Models.Result<Guid>> Invite(string email) =>
        Handler().Handle(new InviteMemberCommand(TestWorkspaceId, email, WorkspaceRole.Member), CancellationToken.None);

    [Fact]
    public async Task InviteMember_WhenTheAddressBelongsToAnActiveMember_IsAConflictAndSendsNothing()
    {
        SetupMembers(CreateMember(WorkspaceRole.Member, _linhId));

        var act = () => Invite("  Linh@Example.com ");

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.ErrorCode.Should().Be("workspaces.invitation.already-member");
        _published.Should().BeEmpty();
        DbContextMock.Verify(c => c.WorkspaceInvitations.Add(It.IsAny<WorkspaceInvitation>()), Times.Never);
    }

    [Fact]
    public async Task InviteMember_WhenTheMemberWasRemoved_IsInvitedAgain()
    {
        var removed = CreateMember(WorkspaceRole.Member, _linhId);
        removed.Remove(activeOwnerCount: 2, TestUserId, TestNow);
        SetupMembers(removed);

        var result = await Invite(Email);

        result.Succeeded.Should().BeTrue();
        _published.Should().ContainSingle();
    }

    [Fact]
    public async Task InviteMember_WhenTheMemberIsSuspended_IsInvitedAgain()
    {
        var suspended = CreateMember(WorkspaceRole.Member, _linhId);
        suspended.Suspend(TestUserId, TestNow, activeOwnerCount: 2);
        SetupMembers(suspended);

        var result = await Invite(Email);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task InviteMember_ForSomeoneWhoIsNotAMember_QueuesTheInvitation()
    {
        SetupMembers(CreateMember(WorkspaceRole.Member, _linhId));

        var result = await Invite("new.person@example.com");

        result.Succeeded.Should().BeTrue();
        _published.Should().ContainSingle()
            .Which.Should().BeOfType<WorkspaceInvitationDeliveryRequestedIntegrationEventV1>()
            .Which.RecipientEmail.Should().Be("new.person@example.com");
    }
}
