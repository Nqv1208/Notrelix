using Notrelix.Domain.Chat.Permissions;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Commands.CreateChannel;
using Notrelix.Application.Features.Chat.Channels.Commands.UpdateChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.ListChannels;
using Notrelix.Application.Features.Chat.Members.Commands.JoinChannel;
using Notrelix.Application.Features.Chat.Members.Commands.RemoveChannelMember;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat;

/// <summary>Channel use cases UC-CHT-A01 to A07 at the handler level: visibility, manager rules, guests, last manager.</summary>
public class ChannelHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Membership> _memberships = [];
    private readonly List<ChannelGroup> _groups = [];
    private readonly List<Message> _messages = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private long _lastSequence;

    public ChannelHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);

        _db.Setup(d => d.ChatSpaces).Returns(TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(TestDbSet.Create(_memberships).Object);
        _db.Setup(d => d.ChatPermissionOverrides).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.PermissionOverrides).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(_groups).Object);
        _db.Setup(d => d.ChatMessages).Returns(TestDbSet.Create(_messages).Object);
        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _sequences.Setup(s => s.NextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => ++_lastSequence);

        SetCaller(_owner, "Owner");
        _members.Setup(m => m.FindAsync(_workspaceId, _member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _guest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_guest, "Guest", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_outsider, "Member", true));
    }

    private void SetCaller(Guid userId, string role)
    {
        _context.Setup(c => c.UserId).Returns(userId);
        _members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, role, true));
    }

    private void AsCaller(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space PublicChannel(string name = "general")
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, name, null, null, 1, _owner, Now);
        _spaces.Add(space);
        return space;
    }

    private Space PrivateChannel(string name = "secret")
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPrivate, name, null, null, 1, _owner, Now);
        _spaces.Add(space);
        return space;
    }

    private CreateChannelCommandHandler CreateHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, AuditFacts());
    private readonly Mock<IAuditService> _auditService = new();
    private ChatAuditFacts AuditFacts() => new(_auditService.Object, _context.Object);
    private void VerifyAudit(string action, Times times) =>
        _auditService.Verify(a => a.RecordAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), action, It.IsAny<ResourceRef>(), It.IsAny<Notrelix.Domain.Common.Auditing.AuditMetadata>(),
            It.IsAny<Notrelix.Domain.Common.Auditing.AuditSeverity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()), times);
    private ChatSystemMessages SystemMessages() => new(_db.Object, _sequences.Object, _clock.Object);
    private JoinChannelCommandHandler JoinHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages());
    private RemoveChannelMemberCommandHandler RemoveHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages(), new Mock<IAccessChangeSignal>().Object, AuditFacts());
    private UpdateChannelCommandHandler UpdateHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages());
    private ListChannelsQueryHandler ListHandler() => new(_db.Object, _context.Object, _members.Object, Views());
    private Notrelix.Application.Features.Chat.Links.Services.ChatResourceViews Views() => new(
        new Notrelix.Testing.Application.Fakes.FakeResourceVisibility(),
        new Mock<Notrelix.Application.Features.Documents.Public.PageViewAccess.IPageViewAccess>().Object,
        new Mock<Notrelix.Application.Features.Documents.Public.PageTitles.IPageTitleFacts>().Object,
        new Mock<Notrelix.Application.Features.WorkManagement.Public.BoardTitles.IBoardTitleFacts>().Object);

    [Fact]
    public async Task CreateChannel_AddsAPublicChannelWithTheCallerAsManager()
    {
        var result = await CreateHandler().Handle(
            new CreateChannelCommand(_workspaceId, "  design ", "Ideas", false, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Name.Should().Be("design");
        result.Data!.Kind.Should().Be("channel_public");
        result.Data!.IsMember.Should().BeTrue();
        result.Data!.Role.Should().Be("manager");
        _spaces.Should().ContainSingle(s => s.Name == "design" && s.IsPublic);
    }

    [Fact]
    public async Task CreateChannel_RefusesADuplicateNameIgnoringCase()
    {
        PublicChannel("General");

        var act = () => CreateHandler().Handle(
            new CreateChannelCommand(_workspaceId, "general", null, false, null), CancellationToken.None);

        var error = await act.Should().ThrowAsync<ConflictException>();
        error.Which.ErrorCode.Should().Be(ChatRuleCodes.NameConflict);
    }

    [Fact]
    public async Task CreateChannel_AllowsTheNameOfAnArchivedChannel()
    {
        var old = PublicChannel("launch");
        old.SetArchived(true, _owner, Now);

        var result = await CreateHandler().Handle(
            new CreateChannelCommand(_workspaceId, "launch", null, false, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task JoinChannel_OfAPrivateChannelIsNotFoundForANonMember()
    {
        var secret = PrivateChannel();
        AsCaller(_outsider);

        var act = () => JoinHandler().Handle(new JoinChannelCommand(_workspaceId, secret.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        secret.IsActiveMember(_outsider).Should().BeFalse();
    }

    [Fact]
    public async Task JoinChannel_IsForbiddenForAGuest()
    {
        var general = PublicChannel();
        AsCaller(_guest);

        var act = () => JoinHandler().Handle(new JoinChannelCommand(_workspaceId, general.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task JoinChannel_AddsAMemberToAPublicChannel()
    {
        var general = PublicChannel();
        AsCaller(_member);

        var result = await JoinHandler().Handle(new JoinChannelCommand(_workspaceId, general.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.IsMember.Should().BeTrue();
        result.Data!.Role.Should().Be("member");
        general.IsActiveMember(_member).Should().BeTrue();
    }

    [Fact]
    public async Task ListChannels_BrowseIsEmptyForAGuest()
    {
        PublicChannel();
        AsCaller(_guest);

        var result = await ListHandler().Handle(new ListChannelsQuery(_workspaceId, Browse: true), CancellationToken.None);

        result.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateChannel_RefusesAPlainMemberOfAPublicChannel()
    {
        var general = PublicChannel();
        general.AddMember(_member, MembershipRole.Member, Now);
        AsCaller(_member);

        var act = () => UpdateHandler().Handle(
            new UpdateChannelCommand(_workspaceId, general.Id, "renamed", null, general.Version), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        general.Name.Should().Be("general");
    }

    [Fact]
    public async Task UpdateChannel_OfAPrivateChannelIsNotFoundForAnOutsider()
    {
        var secret = PrivateChannel();
        AsCaller(_outsider);

        var act = () => UpdateHandler().Handle(
            new UpdateChannelCommand(_workspaceId, secret.Id, "renamed", null, secret.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateChannel_LetsAWorkspaceAdminManageAPublicChannel()
    {
        var general = PublicChannel();
        var admin = Guid.NewGuid();
        SetCaller(admin, "Admin");
        AsCaller(admin);

        var result = await UpdateHandler().Handle(
            new UpdateChannelCommand(_workspaceId, general.Id, "renamed", "New topic", general.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        general.Name.Should().Be("renamed");
    }

    [Fact]
    public async Task RemoveChannelMember_CannotLeaveAsTheLastManager()
    {
        var general = PublicChannel();
        general.AddMember(_member, MembershipRole.Member, Now);
        AsCaller(_owner);

        var act = () => RemoveHandler().Handle(
            new RemoveChannelMemberCommand(_workspaceId, general.Id, _owner), CancellationToken.None);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.LastManager);
        general.IsActiveMember(_owner).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveChannelMember_AMemberMayLeaveOnTheirOwn()
    {
        var general = PublicChannel();
        general.AddMember(_member, MembershipRole.Member, Now);
        AsCaller(_member);

        var result = await RemoveHandler().Handle(
            new RemoveChannelMemberCommand(_workspaceId, general.Id, _member), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        general.IsActiveMember(_member).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveChannelMember_AMemberMayNotRemoveAnotherMember()
    {
        var general = PublicChannel();
        var third = Guid.NewGuid();
        general.AddMember(_member, MembershipRole.Member, Now);
        general.AddMember(third, MembershipRole.Member, Now);
        AsCaller(_member);

        var act = () => RemoveHandler().Handle(
            new RemoveChannelMemberCommand(_workspaceId, general.Id, third), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        general.IsActiveMember(third).Should().BeTrue();
    }

    // ── Channel permission overrides (UC-CHT-H01) ─────────────────────────────

    [Fact]
    public async Task CreateChannel_ReturnsTheCreatorsCapabilities()
    {
        var result = await CreateHandler().Handle(
            new CreateChannelCommand(_workspaceId, "design", null, false, null), CancellationToken.None);

        result.Data!.Capabilities.Should().BeEquivalentTo(new Notrelix.Application.Features.Chat.Permissions.DTOs.ChannelCapabilitiesDto(
            CanPost: true, CanReact: true, CanAttach: true, CanPin: true, CanBroadcast: true, CanInvite: true, CanModerate: true, CanManagePermissions: true));
    }

    [Fact]
    public async Task ListChannels_ShowsAMemberOfAReadOnlyChannelThatTheyCannotPostButCanReact()
    {
        var space = PublicChannel();
        space.AddMember(_member, MembershipRole.Member, Now);
        space.SetPermissionOverrides([new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Post, ChannelPermissionEffect.Deny)], _owner, Now);
        _memberships.AddRange(space.Memberships);
        AsCaller(_member);

        var result = await ListHandler().Handle(new ListChannelsQuery(_workspaceId), CancellationToken.None);

        var caps = result.Data!.Single().Capabilities;
        caps.CanPost.Should().BeFalse();
        caps.CanAttach.Should().BeTrue("attaching is a separate action");
        caps.CanReact.Should().BeTrue();
        caps.CanModerate.Should().BeFalse("a Member does not moderate");
        caps.CanManagePermissions.Should().BeFalse();
    }

    [Fact]
    public async Task ListChannels_AnArchivedChannelIsReadOnlyForEveryone()
    {
        var space = PublicChannel();
        space.SetArchived(true, _owner, Now);
        _memberships.AddRange(space.Memberships);

        var result = await ListHandler().Handle(new ListChannelsQuery(_workspaceId), CancellationToken.None);

        result.Data!.Single().Capabilities.CanPost.Should().BeFalse();
    }

    [Fact]
    public async Task AddMembers_InAChannelThatTurnsInvitingOffForMembers_IsForbidden_ForAMemberOfAPrivateChannel()
    {
        var space = PrivateChannel();
        space.AddMember(_member, MembershipRole.Member, Now);
        space.SetPermissionOverrides([new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Invite, ChannelPermissionEffect.Deny)], _owner, Now);
        AsCaller(_member);

        var act = () => new Notrelix.Application.Features.Chat.Members.Commands.AddChannelMembers.AddChannelMembersCommandHandler(
            _db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages(), AuditFacts())
            .Handle(new Notrelix.Application.Features.Chat.Members.Commands.AddChannelMembers.AddChannelMembersCommand(_workspaceId, space.Id, [_outsider]), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ForbiddenException>();
    }

    [Fact]
    public async Task CreateChannel_WritesOneAuditFact()
    {
        await CreateHandler().Handle(new CreateChannelCommand(_workspaceId, "design", null, false, null), CancellationToken.None);

        VerifyAudit(ChatAuditFacts.ChannelCreated, Times.Once());
    }

    [Fact]
    public async Task RemoveMember_ByAManagerWritesAFact_AndALeaveByThePersonDoesNot()
    {
        var general = PublicChannel();
        general.AddMember(_member, MembershipRole.Member, Now);
        _memberships.AddRange(general.Memberships);

        await RemoveHandler().Handle(new RemoveChannelMemberCommand(_workspaceId, general.Id, _member), CancellationToken.None);
        VerifyAudit(ChatAuditFacts.MemberRemoved, Times.Once());

        general.AddMember(_member, MembershipRole.Member, Now);
        AsCaller(_member);
        await RemoveHandler().Handle(new RemoveChannelMemberCommand(_workspaceId, general.Id, _member), CancellationToken.None);
        VerifyAudit(ChatAuditFacts.MemberRemoved, Times.Once());
    }
}
