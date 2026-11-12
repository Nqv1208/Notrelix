using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Commands.LinkChannel;
using Notrelix.Application.Features.Chat.Channels.Commands.UnlinkChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.GetChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.ListChannels;
using Notrelix.Application.Features.Chat.Links.Queries.ListLinkedChannels;
using Notrelix.Application.Features.Chat.Links.Services;
using Notrelix.Application.Features.Documents.Public.PageTitles;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Features.WorkManagement.Public.BoardTitles;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Links;

/// <summary>UC-CHT-H10: a channel manager links a channel to a Board or Page they can view; the reverse lookup and the views degrade, never fail.</summary>
public class ChannelLinkHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _manager = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _pageId = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IPageViewAccess> _pageAccess = new();
    private readonly Mock<IPageTitleFacts> _pageTitles = new();
    private readonly Mock<IBoardTitleFacts> _boardTitles = new();
    private readonly FakeResourceVisibility _visibility = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Membership> _memberships = [];
    private readonly Mock<IChatDbContext> _db = new();

    public ChannelLinkHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _db.Setup(d => d.ChatSpaces).Returns(TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(TestDbSet.Create(_memberships).Object);
        _db.Setup(d => d.ChatPermissionOverrides).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.PermissionOverrides).ToList()).Object);

        foreach (var (id, role) in new[] { (_owner, "Owner"), (_manager, "Member"), (_member, "Member"), (_guest, "Guest"), (_outsider, "Member") })
            _members.Setup(m => m.FindAsync(_workspaceId, id, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(id, role, true));

        _visibility.Boards.Add(_boardId);
        _boardTitles.Setup(t => t.TitlesAsync(_workspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) => ids.Where(i => i == _boardId).ToDictionary(i => i, _ => "Sprint board"));
        _pageAccess.Setup(p => p.CanViewAsync(_accountId, _workspaceId, _pageId, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _pageTitles.Setup(t => t.TitlesAsync(_workspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) => ids.Where(i => i == _pageId).ToDictionary(i => i, _ => "Runbook"));
        As(_manager);
    }

    private void As(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private ChatResourceViews Views() => new(_visibility, _pageAccess.Object, _pageTitles.Object, _boardTitles.Object);
    private ChatAuditFacts Audit() => new(_auditService.Object, _context.Object);

    private Space Channel(SpaceKind kind = SpaceKind.ChannelPublic, string name = "delivery")
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, name, null, null, 1, _manager, Now);
        _spaces.Add(space);
        _memberships.AddRange(space.Memberships);
        return space;
    }

    private void Join(Space space, Guid userId)
    {
        space.AddMember(userId, MembershipRole.Member, Now);
        _memberships.Add(space.Memberships.Single(m => m.UserId == userId));
    }

    private Task<Notrelix.Application.Common.Models.Result<Notrelix.Application.Features.Chat.Channels.DTOs.ChannelDto>> Link(Space space, string kind, Guid id) =>
        new LinkChannelCommandHandler(_db.Object, _context.Object, _members.Object, _clock.Object, Views(), Audit())
            .Handle(new LinkChannelCommand(_workspaceId, space.Id, kind, id, space.Version), CancellationToken.None);

    private void VerifyAudit(string action, Times times) =>
        _auditService.Verify(a => a.RecordAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), action, It.IsAny<ResourceRef>(), It.IsAny<Notrelix.Domain.Common.Auditing.AuditMetadata>(),
            It.IsAny<Notrelix.Domain.Common.Auditing.AuditSeverity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()), times);

    [Fact]
    public async Task AManagerWhoCanViewABoardLinksIt_TheViewCarriesTitleAndTheAuditFactIsWritten()
    {
        var channel = Channel();
        var version = channel.Version;

        var result = await Link(channel, ChannelLinkKinds.Board, _boardId);

        result.Succeeded.Should().BeTrue();
        result.Data!.Link.Should().BeEquivalentTo(new { Kind = ChannelLinkKinds.Board, ResourceId = _boardId, Accessible = true, Title = "Sprint board" });
        channel.Version.Should().Be(version + 1);
        VerifyAudit(ChatAuditFacts.LinkSet, Times.Once());
    }

    [Fact]
    public async Task APageCanBeLinked_AndASecondLinkReplacesTheFirst()
    {
        var channel = Channel();
        await Link(channel, ChannelLinkKinds.Board, _boardId);

        var result = await Link(channel, ChannelLinkKinds.Page, _pageId);

        result.Data!.Link!.Title.Should().Be("Runbook");
        channel.LinkKind.Should().Be(ChannelLinkKinds.Page);
        VerifyAudit(ChatAuditFacts.LinkSet, Times.Exactly(2));
    }

    [Fact]
    public async Task LinkingTheSameResourceAgainIsANoOpWithoutAnotherAuditFact()
    {
        var channel = Channel();
        await Link(channel, ChannelLinkKinds.Board, _boardId);
        var version = channel.Version;

        await Link(channel, ChannelLinkKinds.Board, _boardId);

        channel.Version.Should().Be(version);
        VerifyAudit(ChatAuditFacts.LinkSet, Times.Once());
    }

    [Fact]
    public async Task AManagerWhoCannotViewTheResourceIsForbidden_AndNothingIsStored()
    {
        var channel = Channel();
        var hiddenBoard = Guid.NewGuid();
        _pageAccess.Setup(p => p.CanViewAsync(_accountId, _workspaceId, _pageId, _manager, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await FluentActions.Awaiting(() => Link(channel, ChannelLinkKinds.Board, hiddenBoard)).Should().ThrowAsync<ForbiddenException>();
        await FluentActions.Awaiting(() => Link(channel, ChannelLinkKinds.Page, _pageId)).Should().ThrowAsync<ForbiddenException>();

        channel.HasLink.Should().BeFalse();
        VerifyAudit(ChatAuditFacts.LinkSet, Times.Never());
    }

    [Fact]
    public async Task APageThatWasDeletedIsTheSame403AsAHiddenOne()
    {
        var channel = Channel();
        _pageTitles.Setup(t => t.TitlesAsync(_workspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());

        await FluentActions.Awaiting(() => Link(channel, ChannelLinkKinds.Page, _pageId)).Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task APlainMemberAndAGuestCannotLink()
    {
        var channel = Channel();
        Join(channel, _member);
        Join(channel, _guest);

        As(_member);
        await FluentActions.Awaiting(() => Link(channel, ChannelLinkKinds.Board, _boardId)).Should().ThrowAsync<ForbiddenException>();
        As(_guest);
        await FluentActions.Awaiting(() => Link(channel, ChannelLinkKinds.Board, _boardId)).Should().ThrowAsync<ForbiddenException>();

        channel.HasLink.Should().BeFalse();
    }

    [Fact]
    public async Task AWorkspaceOwnerWhoIsNotInAPrivateChannelCannotReachIt()
    {
        var secret = Channel(SpaceKind.ChannelPrivate, "secret");
        As(_owner);

        await FluentActions.Awaiting(() => Link(secret, ChannelLinkKinds.Board, _boardId)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task TheTitleOfAResourceTheCallerCannotViewIsNeverRead()
    {
        var channel = Channel();
        var hiddenBoard = Guid.NewGuid();

        await FluentActions.Awaiting(() => Link(channel, ChannelLinkKinds.Board, hiddenBoard)).Should().ThrowAsync<ForbiddenException>();

        _boardTitles.Verify(t => t.TitlesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnlinkRemovesTheLinkAndWritesTheAuditFact_EvenWhenTheResourceIsNowHidden()
    {
        var channel = Channel();
        await Link(channel, ChannelLinkKinds.Board, _boardId);
        _visibility.Boards.Clear();

        var result = await new UnlinkChannelCommandHandler(_db.Object, _context.Object, _members.Object, _clock.Object, Audit())
            .Handle(new UnlinkChannelCommand(_workspaceId, channel.Id, channel.Version), CancellationToken.None);

        result.Data!.Link.Should().BeNull();
        channel.HasLink.Should().BeFalse();
        VerifyAudit(ChatAuditFacts.LinkCleared, Times.Once());
    }

    [Fact]
    public async Task UnlinkingAChannelWithoutALinkIsANoOp()
    {
        var channel = Channel();
        var version = channel.Version;

        var result = await new UnlinkChannelCommandHandler(_db.Object, _context.Object, _members.Object, _clock.Object, Audit())
            .Handle(new UnlinkChannelCommand(_workspaceId, channel.Id, channel.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        channel.Version.Should().Be(version);
        VerifyAudit(ChatAuditFacts.LinkCleared, Times.Never());
    }

    [Fact]
    public async Task AResourceHiddenLaterDegradesToAGenericChip_InTheChannelAndInTheList()
    {
        var channel = Channel();
        await Link(channel, ChannelLinkKinds.Board, _boardId);
        _visibility.Boards.Clear();

        var get = await new GetChannelQueryHandler(_db.Object, _context.Object, _members.Object, Views())
            .Handle(new GetChannelQuery(_workspaceId, channel.Id), CancellationToken.None);
        var list = await new ListChannelsQueryHandler(_db.Object, _context.Object, _members.Object, Views())
            .Handle(new ListChannelsQuery(_workspaceId), CancellationToken.None);

        get.Succeeded.Should().BeTrue();
        get.Data!.Channel.Link.Should().BeEquivalentTo(new { Kind = ChannelLinkKinds.Board, ResourceId = _boardId, Accessible = false, Title = (string?)null });
        list.Data!.Single().Link!.Accessible.Should().BeFalse();
    }

    [Fact]
    public async Task ATeamLinkThatExistsDegradesToGeneric_AndANewOneIsNotLinkable()
    {
        ChatResourceViews.IsLinkable(ChannelLinkKinds.Team).Should().BeFalse("Governance publishes no Team visibility fact yet");
        var channel = Channel();
        channel.SetLink(ChannelLinkKinds.Team, Guid.NewGuid(), _manager, Now);

        var get = await new GetChannelQueryHandler(_db.Object, _context.Object, _members.Object, Views())
            .Handle(new GetChannelQuery(_workspaceId, channel.Id), CancellationToken.None);

        get.Data!.Channel.Link!.Accessible.Should().BeFalse();
        get.Data.Channel.Link.Title.Should().BeNull();
    }

    [Fact]
    public void TheValidatorRefusesATeamAndAnUnknownKind()
    {
        var validator = new LinkChannelCommandValidator();

        validator.Validate(new LinkChannelCommand(_workspaceId, Guid.NewGuid(), ChannelLinkKinds.Team, _boardId, 1)).IsValid.Should().BeFalse();
        validator.Validate(new LinkChannelCommand(_workspaceId, Guid.NewGuid(), "work-management.board-item", _boardId, 1)).IsValid.Should().BeFalse();
        validator.Validate(new LinkChannelCommand(_workspaceId, Guid.NewGuid(), ChannelLinkKinds.Board, _boardId, 1)).IsValid.Should().BeTrue();
    }

    private Task<Notrelix.Application.Common.Models.Result<IReadOnlyList<Notrelix.Application.Features.Chat.Links.DTOs.LinkedChannelDto>>> ListLinked(string kind, Guid id) =>
        new ListLinkedChannelsQueryHandler(_db.Object, _context.Object, _members.Object, Views())
            .Handle(new ListLinkedChannelsQuery(_workspaceId, kind, id), CancellationToken.None);

    [Fact]
    public async Task ListLinked_ReturnsOnlyChannelsTheCallerCanSee()
    {
        var open = Channel(SpaceKind.ChannelPublic, "open");
        var secretIn = Channel(SpaceKind.ChannelPrivate, "secret-in");
        var secretOut = Channel(SpaceKind.ChannelPrivate, "secret-out");
        var unlinked = Channel(SpaceKind.ChannelPublic, "other");
        foreach (var c in new[] { open, secretIn, secretOut }) c.SetLink(ChannelLinkKinds.Board, _boardId, _manager, Now);
        Join(secretIn, _member);

        As(_member);
        var result = await ListLinked(ChannelLinkKinds.Board, _boardId);

        result.Data!.Select(c => c.Name).Should().Equal("open", "secret-in");
        result.Data.Single(c => c.Name == "secret-in").IsMember.Should().BeTrue();
        result.Data.Single(c => c.Name == "open").IsMember.Should().BeFalse();
        unlinked.HasLink.Should().BeFalse();
    }

    [Fact]
    public async Task ListLinked_AGuestSeesOnlyChannelsTheyWereAddedTo()
    {
        var open = Channel(SpaceKind.ChannelPublic, "open");
        var added = Channel(SpaceKind.ChannelPublic, "added");
        foreach (var c in new[] { open, added }) c.SetLink(ChannelLinkKinds.Board, _boardId, _manager, Now);
        Join(added, _guest);

        As(_guest);
        var result = await ListLinked(ChannelLinkKinds.Board, _boardId);

        result.Data!.Select(c => c.Name).Should().Equal("added");
    }

    [Fact]
    public async Task ListLinked_IsEmptyForACallerWhoCannotViewTheResource()
    {
        var open = Channel(SpaceKind.ChannelPublic, "open");
        open.SetLink(ChannelLinkKinds.Board, _boardId, _manager, Now);
        _visibility.Boards.Clear();

        var result = await ListLinked(ChannelLinkKinds.Board, _boardId);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ListLinked_NamesAnArchivedChannelWithItsState()
    {
        var open = Channel(SpaceKind.ChannelPublic, "old");
        open.SetLink(ChannelLinkKinds.Board, _boardId, _manager, Now);
        open.SetArchived(true, _manager, Now);

        var result = await ListLinked(ChannelLinkKinds.Board, _boardId);

        result.Data!.Single().IsArchived.Should().BeTrue();
    }
}
