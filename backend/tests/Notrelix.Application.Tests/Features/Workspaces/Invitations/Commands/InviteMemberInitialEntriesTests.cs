using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

/// <summary>
/// The initial Team and Space entries of an invitation (WSP-026) are checked when the invitation is issued: each must
/// name an active Team or Space of this Workspace, a Guest cannot name a Team lead or a Space admin, and a refused
/// invitation stores nothing and sends nothing. Who may name entries at all is the pipeline's rule
/// (<c>InvitationMixedEntriesAuthorizationIntegrationTests</c>).
/// </summary>
public class InviteMemberInitialEntriesTests : WorkspaceHandlerTestBase
{
    private readonly Mock<ISpaceStore> _store = new();
    private readonly Mock<IOneTimeTokenService> _tokens = new();
    private readonly Mock<IIntegrationEventCollector> _events = new();
    private readonly List<IIntegrationEvent> _published = [];

    public InviteMemberInitialEntriesTests()
    {
        _events.Setup(e => e.Add(It.IsAny<IIntegrationEvent>())).Callback<IIntegrationEvent>(_published.Add);
        _tokens.Setup(t => t.Generate(TokenPurpose.WorkspaceInvitation))
            .Returns(new IssuedOneTimeToken("raw-token", new string('a', 64), 1));
        var encryptor = new Mock<ISecretEncryptor>();
        encryptor.Setup(e => e.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("protected");
        _encryptor = encryptor;
        _memberEmails.Setup(m => m.ResolveAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
    }

    private readonly Mock<ISecretEncryptor> _encryptor;
    private readonly Mock<IMemberEmailDisclosure> _memberEmails = new();

    private InviteMemberCommandHandler Handler() => new(
        DbContextMock.Object, new Mock<IActorLookupService>().Object, RequestContextMock.Object, DateTimeProviderMock.Object,
        _tokens.Object, _encryptor.Object, _events.Object,
        new InvitationInitialEntries(DbContextMock.Object, _store.Object),
        _memberEmails.Object);

    private Task<Notrelix.Application.Common.Models.Result<Guid>> Invite(
        WorkspaceRole role, params InvitationInitialEntryInput[] entries) =>
        Handler().Handle(new InviteMemberCommand(TestWorkspaceId, "new.person@example.com", role, entries), CancellationToken.None);

    private void NothingStoredOrSent()
    {
        _published.Should().BeEmpty();
        DbContextMock.Verify(c => c.WorkspaceInvitations.Add(It.IsAny<WorkspaceInvitation>()), Times.Never);
    }

    [Fact]
    public async Task ASpaceOfAnotherWorkspace_IsRefused_AndNothingIsStoredOrSent()
    {
        var foreign = Space.Create(TestAccountId, Guid.CreateVersion7(), "Foreign", SpaceVisibility.Workspace, TestUserId, TestNow, SpaceType.Folder);
        SetupSpaces(foreign);

        var act = () => Invite(WorkspaceRole.Member, new InvitationInitialEntryInput("Space", foreign.Id, "Member"));

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Invitation_EntryTargetNotActive);
        NothingStoredOrSent();
    }

    [Fact]
    public async Task AnArchivedSpace_IsRefused_AndNothingIsStoredOrSent()
    {
        var archived = CreateSpace();
        archived.Archive(TestUserId, TestNow, SpaceContentFact.Empty);
        SetupSpaces(archived);

        var act = () => Invite(WorkspaceRole.Member, new InvitationInitialEntryInput("Space", archived.Id, "Member"));

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Invitation_EntryTargetNotActive);
        NothingStoredOrSent();
    }

    [Fact]
    public async Task ATeamThatIsNotActive_IsRefused_AndNothingIsStoredOrSent()
    {
        var teamId = Guid.CreateVersion7();
        _store.Setup(s => s.IsActiveTeamAsync(TestWorkspaceId, teamId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => Invite(WorkspaceRole.Member, new InvitationInitialEntryInput("Team", teamId, "Member"));

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Invitation_EntryTargetNotActive);
        NothingStoredOrSent();
    }

    [Theory]
    [InlineData("Team", "Lead")]
    [InlineData("Space", "Admin")]
    public async Task AGuestInvitation_CannotNameATeamLeadOrASpaceAdmin_AndNothingIsStoredOrSent(string kind, string role)
    {
        var space = CreateSpace();
        SetupSpaces(space);
        var teamId = Guid.CreateVersion7();
        _store.Setup(s => s.IsActiveTeamAsync(TestWorkspaceId, teamId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var target = kind == "Team" ? teamId : space.Id;

        var act = () => Invite(WorkspaceRole.Guest, new InvitationInitialEntryInput(kind, target, role));

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Invitation_GuestEntryRoleTooPrivileged);
        NothingStoredOrSent();
    }

    private static IReadOnlyList<InvitationEntryRefusal> Refusals(Notrelix.Domain.Common.Exceptions.BusinessRuleException ex) =>
        (IReadOnlyList<InvitationEntryRefusal>)ex.Details["entries"]!;

    [Fact]
    public async Task OnlyTheEntriesThatAreRefused_AreNamed_EachWithItsPositionKindAndTarget()
    {
        var open = CreateSpace();
        var archived = CreateSpace();
        archived.Archive(TestUserId, TestNow, SpaceContentFact.Empty);
        SetupSpaces(open, archived);
        var activeTeam = Guid.CreateVersion7();
        var goneTeam = Guid.CreateVersion7();
        _store.Setup(s => s.IsActiveTeamAsync(TestWorkspaceId, activeTeam, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _store.Setup(s => s.IsActiveTeamAsync(TestWorkspaceId, goneTeam, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => Invite(
            WorkspaceRole.Member,
            new InvitationInitialEntryInput("Team", activeTeam, "Member"),
            new InvitationInitialEntryInput("Team", goneTeam, "Lead"),
            new InvitationInitialEntryInput("Space", open.Id, "Viewer"),
            new InvitationInitialEntryInput("Space", archived.Id, "Member"));

        var refused = Refusals((await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which);
        refused.Should().Equal(
            new InvitationEntryRefusal(1, "team", goneTeam, InvitationEntryRefusal.NotActive),
            new InvitationEntryRefusal(3, "space", archived.Id, InvitationEntryRefusal.NotActive));
        NothingStoredOrSent();
    }

    [Fact]
    public async Task AGuestInvitation_NamesEachEntryThatIsTooPrivileged()
    {
        var space = CreateSpace();
        SetupSpaces(space);
        var teamId = Guid.CreateVersion7();
        _store.Setup(s => s.IsActiveTeamAsync(TestWorkspaceId, teamId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => Invite(
            WorkspaceRole.Guest,
            new InvitationInitialEntryInput("Team", teamId, "Lead"),
            new InvitationInitialEntryInput("Space", space.Id, "Viewer"));

        var refused = Refusals((await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which);
        refused.Should().Equal(new InvitationEntryRefusal(0, "team", teamId, InvitationEntryRefusal.GuestTooPrivileged));
        NothingStoredOrSent();
    }

    [Fact]
    public async Task ActiveTargets_AreStoredWithTheInvitation_AndTheInvitationIsSent()
    {
        var space = CreateSpace();
        SetupSpaces(space);
        var teamId = Guid.CreateVersion7();
        _store.Setup(s => s.IsActiveTeamAsync(TestWorkspaceId, teamId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        WorkspaceInvitation? stored = null;
        DbContextMock.Setup(c => c.WorkspaceInvitations.Add(It.IsAny<WorkspaceInvitation>()))
            .Callback<WorkspaceInvitation>(i => stored = i);

        var result = await Invite(
            WorkspaceRole.Member,
            new InvitationInitialEntryInput("Team", teamId, "Lead"),
            new InvitationInitialEntryInput("Space", space.Id, "Viewer"));

        result.Succeeded.Should().BeTrue();
        stored!.InitialEntries.Select(e => (e.Kind.ToString(), e.TargetId, e.Role)).Should().BeEquivalentTo(
            new[] { ("Team", teamId, "Lead"), ("Space", space.Id, "Viewer") });
        _published.Should().ContainSingle();
    }
}
