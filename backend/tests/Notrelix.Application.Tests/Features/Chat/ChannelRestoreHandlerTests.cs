using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Commands.RestoreChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.ListDeletedChannels;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat;

/// <summary>Restoring a deleted channel (UC-CHT-A08): the delete permission rule, the 30-day window, the name, the version, and who sees the list.</summary>
public class ChannelRestoreHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DeletedAt = Now.AddDays(-3);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _manager = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _admin = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IAccessChangeSignal> _signal = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Membership> _memberships = [];
    private readonly Mock<IChatDbContext> _db = new();

    public ChannelRestoreHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _db.Setup(d => d.ChatSpaces).Returns(TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(TestDbSet.Create(_memberships).Object);
        _db.Setup(d => d.ChatPermissionOverrides).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.PermissionOverrides).ToList()).Object);
        Fact(_manager, "Member");
        Fact(_member, "Member");
        Fact(_guest, "Guest");
        Fact(_admin, "Admin");
        Fact(_outsider, "Member");
        As(_manager);
    }

    private void Fact(Guid userId, string role) =>
        _members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(userId, role, true));

    private void As(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    /// <summary>A deleted channel created by <c>_manager</c>, so the manager is its active manager; <paramref name="deletedAt"/> sets the delete time.</summary>
    private Space Deleted(SpaceKind kind = SpaceKind.ChannelPublic, string name = "general", DateTimeOffset? deletedAt = null, bool guestIsManager = false)
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, name, null, null, 1, _manager, Now.AddDays(-40));
        space.AddMember(_member, MembershipRole.Member, Now.AddDays(-39));
        if (guestIsManager) space.AddMember(_guest, MembershipRole.Manager, Now.AddDays(-38));
        _spaces.Add(space);
        _memberships.AddRange(space.Memberships);
        space.Delete(_manager, deletedAt ?? DeletedAt);
        return space;
    }

    private RestoreChannelCommandHandler Handler() => new(
        _db.Object, _context.Object, _members.Object, _clock.Object, _signal.Object, new ChatAuditFacts(_auditService.Object, _context.Object));

    private ListDeletedChannelsQueryHandler ListHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);

    private Task<Notrelix.Application.Common.Models.Result> Restore(Space space, long? version = null) =>
        Handler().Handle(new RestoreChannelCommand(_workspaceId, space.Id, version ?? space.Version), CancellationToken.None);

    [Fact]
    public async Task AManagerRestoresAChannelInsideTheWindow_AndItsMembersAreExactlyAsTheyWere()
    {
        var space = Deleted();
        var before = space.Memberships.Select(m => (m.UserId, m.Role, m.IsActive)).OrderBy(m => m.UserId).ToList();
        var version = space.Version;

        var result = await Restore(space);

        result.Succeeded.Should().BeTrue();
        space.IsDeleted.Should().BeFalse();
        space.Version.Should().BeGreaterThan(version);
        space.Memberships.Select(m => (m.UserId, m.Role, m.IsActive)).OrderBy(m => m.UserId).Should().Equal(before, "no member is added or removed by a restore");
        _signal.Verify(s => s.Channel(_workspaceId, space.Id), Times.Once);
    }

    [Fact]
    public async Task ARestoreIsAuditedOnce()
    {
        var space = Deleted();

        await Restore(space);

        _auditService.Verify(a => a.RecordAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), ChatAuditFacts.ChannelRestored, It.IsAny<ResourceRef>(), It.IsAny<Notrelix.Domain.Common.Auditing.AuditMetadata>(),
            It.IsAny<Notrelix.Domain.Common.Auditing.AuditSeverity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()), Times.Once);
    }

    [Fact]
    public async Task AMemberWhoIsNotAManager_IsForbidden_AndTheChannelStaysDeleted()
    {
        var space = Deleted();
        As(_member);

        var act = () => Restore(space);

        await act.Should().ThrowAsync<ForbiddenException>();
        space.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task APublicChannelIsForbiddenToAGuest_WhoIsNeverAManager()
    {
        var space = Deleted(guestIsManager: true);
        As(_guest);

        var act = () => Restore(space);

        await act.Should().ThrowAsync<Exception>().Where(e => e is ForbiddenException || e is NotFoundException);
        space.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task AWorkspaceAdminWhoIsNotInAPrivateChannel_GetsTheSameNotFoundAsForAMissingOne()
    {
        var space = Deleted(SpaceKind.ChannelPrivate, "secret");
        As(_admin);

        var act = () => Restore(space);

        await act.Should().ThrowAsync<NotFoundException>();
        space.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task AWorkspaceAdminRestoresAPublicChannelByRole()
    {
        var space = Deleted();
        As(_admin);

        var result = await Restore(space);

        result.Succeeded.Should().BeTrue();
        space.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task AChannelOfAnotherWorkspace_IsNotFound()
    {
        var space = Deleted();
        var act = () => Handler().Handle(new RestoreChannelCommand(Guid.NewGuid(), space.Id, space.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        space.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ALiveChannel_IsNotFound_ForRestore()
    {
        var space = Deleted();
        space.Restore(_manager, Now);

        var act = () => Restore(space);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task APastTheWindowChannel_IsRefusedWithAStableCode_AndStaysDeleted()
    {
        var space = Deleted(deletedAt: Now.AddDays(-31));

        var act = () => Restore(space);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be(ChatRuleCodes.RestoreWindowElapsed);
        space.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ADeletedChannelWhoseNameWasTaken_IsRefusedWithTheNameConflictCode()
    {
        var space = Deleted(name: "general");
        var taker = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "General", null, null, 2, _manager, Now.AddDays(-1));
        _spaces.Add(taker);

        var act = () => Restore(space);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be(ChatRuleCodes.NameConflict);
        space.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ADirectOrGroupConversation_CannotBeRestoredAsAChannel()
    {
        var conversation = Space.CreateDirect(_accountId, _workspaceId, _manager, _member, Now.AddDays(-40));
        _spaces.Add(conversation);
        conversation.Delete(_manager, DeletedAt);

        var act = () => Restore(conversation);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AStaleVersionIsDeclaredByTheCommandAsTheConcurrencyResource()
    {
        var space = Deleted();
        IExpectedVersionRequest command = new RestoreChannelCommand(_workspaceId, space.Id, 7);

        command.ExpectedVersion.Should().Be(7);
        command.Resource.Kind.Value.Should().Be("chat.channel");
        command.Resource.ResourceId.Should().Be(space.Id);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task TheListHoldsOnlyWhatTheCallerCouldRestore_WithTheLastRestoreDate()
    {
        var publicOne = Deleted(name: "ideas");
        var privateOne = Deleted(SpaceKind.ChannelPrivate, "board-only");
        Deleted(name: "too-old", deletedAt: Now.AddDays(-31));

        var asManager = await ListHandler().Handle(new ListDeletedChannelsQuery(_workspaceId), CancellationToken.None);
        asManager.Data!.Select(c => c.Name).Should().BeEquivalentTo("ideas", "board-only");
        asManager.Data!.Single(c => c.Id == publicOne.Id).RestorableUntil.Should().Be(DeletedAt.AddDays(30));

        As(_admin);
        var asAdmin = await ListHandler().Handle(new ListDeletedChannelsQuery(_workspaceId), CancellationToken.None);
        asAdmin.Data!.Select(c => c.Name).Should().BeEquivalentTo("ideas").And.NotContain("board-only", "a private channel's name never reaches an Admin who is not in it");

        As(_member);
        (await ListHandler().Handle(new ListDeletedChannelsQuery(_workspaceId), CancellationToken.None)).Data.Should().BeEmpty();

        As(_guest);
        (await ListHandler().Handle(new ListDeletedChannelsQuery(_workspaceId), CancellationToken.None)).Data.Should().BeEmpty();
        privateOne.IsDeleted.Should().BeTrue();
    }
}
