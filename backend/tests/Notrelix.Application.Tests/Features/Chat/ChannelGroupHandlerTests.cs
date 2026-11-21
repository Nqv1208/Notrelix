using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.ChannelGroups.Commands.CreateChannelGroup;
using Notrelix.Application.Features.Chat.ChannelGroups.Commands.DeleteChannelGroup;
using Notrelix.Application.Features.Chat.ChannelGroups.Commands.UpdateChannelGroup;
using Notrelix.Application.Features.Chat.ChannelGroups.Queries.ListChannelGroups;
using Notrelix.Application.Features.Chat.Channels.Commands.ReorderChannels;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat;

/// <summary>
/// UC-CHT-A02 and A04 at handler level: who may manage categories and reorder channels (the role baseline the pipeline reads and the
/// per-channel manager rule the reorder handler enforces), the unique name, the Workspace scope of every id, the version rule of a
/// batch, and what deleting a category does to its channels.
/// </summary>
public class ChannelGroupHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _otherWorkspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _admin = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<ChannelGroup> _groups = [];
    private readonly Mock<IChatDbContext> _db = new();

    public ChannelGroupHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _context.Setup(c => c.UserId).Returns(_owner);

        _db.Setup(d => d.ChatSpaces).Returns(TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatPermissionOverrides).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.PermissionOverrides).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(_groups).Object);

        _members.Setup(m => m.FindAsync(_workspaceId, _owner, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(_owner, "Owner", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _admin, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(_admin, "Admin", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _member, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _guest, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(_guest, "Guest", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>())).ReturnsAsync((WorkspaceMemberFact?)null);
    }

    private void AsCaller(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private CreateChannelGroupCommandHandler CreateHandler() => new(_db.Object, _context.Object, _clock.Object);
    private UpdateChannelGroupCommandHandler UpdateHandler() => new(_db.Object, _context.Object, _clock.Object);
    private DeleteChannelGroupCommandHandler DeleteHandler() => new(_db.Object, _context.Object, _clock.Object);
    private ReorderChannelsCommandHandler ReorderHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);

    private ChannelGroup Group(string name, double position, Guid? workspaceId = null)
    {
        var group = ChannelGroup.Create(_accountId, workspaceId ?? _workspaceId, name, position, _owner, Now);
        _groups.Add(group);
        return group;
    }

    private Space Channel(string name, Guid? groupId = null, Guid? workspaceId = null, SpaceKind kind = SpaceKind.ChannelPublic)
    {
        var space = Space.CreateChannel(_accountId, workspaceId ?? _workspaceId, kind, name, null, groupId, 1, _owner, Now);
        _spaces.Add(space);
        return space;
    }

    // ---- Who may manage categories: the Governance baseline the pipeline reads, and the action each command declares ----------

    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Admin", true)]
    [InlineData("Member", false)]
    [InlineData("Guest", false)]
    public void ManagingCategories_IsHeldByOwnerAndAdminByDefault_NotByAMemberOrAGuest(string role, bool allowed)
    {
        WorkspaceRoleBaseline.Decide(role, PermissionAction.ChatGroupManage)
            .Should().Be(allowed ? BaselineDecision.Allow : BaselineDecision.Deny);
    }

    [Fact]
    public void EveryCategoryWrite_DeclaresTheManageAction_AndReorderTheChannelManageAction()
    {
        new CreateChannelGroupCommand(_workspaceId, "x", null).Action.Should().Be(PermissionAction.ChatGroupManage);
        new UpdateChannelGroupCommand(_workspaceId, Guid.NewGuid(), "x", 1, 1).Action.Should().Be(PermissionAction.ChatGroupManage);
        new DeleteChannelGroupCommand(_workspaceId, Guid.NewGuid(), 1).Action.Should().Be(PermissionAction.ChatGroupManage);
        new ReorderChannelsCommand(_workspaceId, []).Action.Should().Be(PermissionAction.ChatChannelManage);
    }

    // ---- Create ------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_AddsACategoryAtTheEndOfTheSidebar_WithTheTrimmedName()
    {
        Group("Projects", 4);

        var result = await CreateHandler().Handle(new CreateChannelGroupCommand(_workspaceId, "  Teams ", null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Name.Should().Be("Teams");
        result.Data.Position.Should().Be(5, "the next position after the last category");
        _db.Verify(d => d.ChatChannelGroups, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Create_ARepeatedNameIgnoringCase_IsANameConflict()
    {
        Group("Projects", 1);

        var act = () => CreateHandler().Handle(new CreateChannelGroupCommand(_workspaceId, "  projects ", null), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be(ChatRuleCodes.NameConflict);
    }

    [Fact]
    public async Task Create_TheSameNameInAnotherWorkspace_IsNotAConflict()
    {
        Group("Projects", 1, _otherWorkspaceId);

        var result = await CreateHandler().Handle(new CreateChannelGroupCommand(_workspaceId, "Projects", null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Create_Validator_RefusesAnEmptyOrTooLongNameAndANonFinitePosition()
    {
        var validator = new CreateChannelGroupCommandValidator();

        validator.Validate(new CreateChannelGroupCommand(_workspaceId, " ", null)).IsValid.Should().BeFalse();
        validator.Validate(new CreateChannelGroupCommand(_workspaceId, new string('a', ChatLimits.ChannelGroupNameMaxLength + 1), null)).IsValid.Should().BeFalse();
        validator.Validate(new CreateChannelGroupCommand(_workspaceId, "ok", double.NaN)).IsValid.Should().BeFalse();
        validator.Validate(new CreateChannelGroupCommand(_workspaceId, "ok", 2)).IsValid.Should().BeTrue();
    }

    // ---- Update ------------------------------------------------------------------------------------

    [Fact]
    public async Task Update_RenamesAndMovesTheCategory_AndRaisesItsVersion()
    {
        var group = Group("Projects", 1);
        var before = group.Version;

        var result = await UpdateHandler().Handle(new UpdateChannelGroupCommand(_workspaceId, group.Id, "Work", 3.5, before), CancellationToken.None);

        result.Data!.Name.Should().Be("Work");
        result.Data.Position.Should().Be(3.5);
        result.Data.Version.Should().BeGreaterThan(before, "a later write with the old version is refused by the version check");
    }

    [Fact]
    public async Task Update_ANameAnotherCategoryHolds_IsANameConflict_AndItsOwnNameIsNot()
    {
        var group = Group("Projects", 1);
        Group("Teams", 2);

        var clash = () => UpdateHandler().Handle(new UpdateChannelGroupCommand(_workspaceId, group.Id, "TEAMS", 1, group.Version), CancellationToken.None);
        var same = await UpdateHandler().Handle(new UpdateChannelGroupCommand(_workspaceId, group.Id, "projects", 9, group.Version), CancellationToken.None);

        await clash.Should().ThrowAsync<ConflictException>();
        same.Succeeded.Should().BeTrue("a move that keeps the name is not a clash with itself");
    }

    [Fact]
    public async Task Update_ACategoryOfAnotherWorkspace_IsNotFound_AndNothingChanges()
    {
        var foreign = Group("Foreign", 1, _otherWorkspaceId);

        var act = () => UpdateHandler().Handle(new UpdateChannelGroupCommand(_workspaceId, foreign.Id, "Hijacked", 1, foreign.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        foreign.Name.Should().Be("Foreign");
    }

    // ---- Delete ------------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_ACategoryWithChannels_LeavesTheChannelsInPlaceOutsideAnyCategory()
    {
        var group = Group("Projects", 1);
        var inside = Channel("erp", group.Id);
        var other = Channel("hr", Group("Teams", 2).Id);
        var archivedOrOutside = Channel("random");

        var result = await DeleteHandler().Handle(new DeleteChannelGroupCommand(_workspaceId, group.Id, group.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        inside.GroupId.Should().BeNull("the channel is not deleted, it leaves the category");
        inside.DeletedAt.Should().BeNull();
        other.GroupId.Should().NotBeNull("another category's channels are untouched");
        archivedOrOutside.GroupId.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ACategoryOfAnotherWorkspace_IsNotFound_AndItsChannelsKeepTheirCategory()
    {
        var foreign = Group("Foreign", 1, _otherWorkspaceId);
        var foreignChannel = Channel("theirs", foreign.Id, _otherWorkspaceId);

        var act = () => DeleteHandler().Handle(new DeleteChannelGroupCommand(_workspaceId, foreign.Id, foreign.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        foreignChannel.GroupId.Should().Be(foreign.Id);
    }

    // ---- List --------------------------------------------------------------------------------------

    [Fact]
    public async Task List_ReadsOnlyTheCallersWorkspace_InPositionOrder()
    {
        Group("B", 2);
        Group("A", 1);
        Group("Foreign", 0, _otherWorkspaceId);
        AsCaller(_member);

        var result = await new ListChannelGroupsQueryHandler(_db.Object, _context.Object, _members.Object)
            .Handle(new ListChannelGroupsQuery(_workspaceId), CancellationToken.None);

        result.Data!.Select(g => g.Name).Should().Equal("A", "B");
    }

    // ---- Reorder: the manager rule per channel, scope, versions ---------------------------------------

    [Fact]
    public async Task Reorder_AnAdminMovesChannelsIntoACategory_AndTheOrderIsStored()
    {
        AsCaller(_admin);
        var group = Group("Projects", 1);
        var a = Channel("a");
        var b = Channel("b");

        var result = await ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId,
        [
            new ChannelPlacement(a.Id, group.Id, 2, a.Version),
            new ChannelPlacement(b.Id, group.Id, 1, b.Version),
        ]), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        (a.GroupId, a.Position).Should().Be((group.Id, 2d));
        (b.GroupId, b.Position).Should().Be((group.Id, 1d));
    }

    [Fact]
    public async Task Reorder_AMemberWhoDoesNotManageTheChannel_IsForbidden_AndNothingMoves()
    {
        AsCaller(_member);
        var channel = Channel("a");
        channel.AddMember(_member, MembershipRole.Member, Now);

        var act = () => ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId,
            [new ChannelPlacement(channel.Id, null, 9, channel.Version)]), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        channel.Position.Should().Be(1);
    }

    [Fact]
    public async Task Reorder_ACreatorWhoManagesTheChannel_MayMoveIt_ButNotAChannelTheyDoNotManage()
    {
        AsCaller(_member);
        var mine = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "mine", null, null, 1, _member, Now);
        _spaces.Add(mine);
        var theirs = Channel("theirs");

        (await ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId, [new ChannelPlacement(mine.Id, null, 5, mine.Version)]), CancellationToken.None))
            .Succeeded.Should().BeTrue();

        var act = () => ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId,
        [
            new ChannelPlacement(mine.Id, null, 6, mine.Version),
            new ChannelPlacement(theirs.Id, null, 6, theirs.Version),
        ]), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
        mine.Position.Should().Be(5, "one refused channel refuses the whole batch");
    }

    [Fact]
    public async Task Reorder_AGuest_IsRefused()
    {
        AsCaller(_guest);
        var channel = Channel("a");
        channel.AddMember(_guest, MembershipRole.Manager, Now);

        var act = () => ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId,
            [new ChannelPlacement(channel.Id, null, 9, channel.Version)]), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>("a Guest never manages a channel, whatever role a row holds");
        channel.Position.Should().Be(1);
    }

    [Fact]
    public async Task Reorder_APersonOutsideTheWorkspace_IsNotFound()
    {
        AsCaller(_outsider);
        var channel = Channel("a");

        var act = () => ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId,
            [new ChannelPlacement(channel.Id, null, 9, channel.Version)]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        channel.Position.Should().Be(1);
    }

    [Fact]
    public async Task Reorder_AStaleVersionOfAnyChannel_RefusesTheWholeBatch_AndChangesNothing()
    {
        AsCaller(_admin);
        var a = Channel("a");
        var b = Channel("b");

        var act = () => ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId,
        [
            new ChannelPlacement(a.Id, null, 7, a.Version),
            new ChannelPlacement(b.Id, null, 8, b.Version + 1),
        ]), CancellationToken.None);

        await act.Should().ThrowAsync<PreconditionFailedException>();
        a.Position.Should().Be(1);
        b.Position.Should().Be(1);
    }

    [Fact]
    public async Task Reorder_AChannelOfAnotherWorkspace_IsNotFound_AndTheBatchChangesNothing()
    {
        AsCaller(_admin);
        var mine = Channel("mine");
        var foreign = Channel("foreign", null, _otherWorkspaceId);

        var act = () => ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId,
        [
            new ChannelPlacement(mine.Id, null, 4, mine.Version),
            new ChannelPlacement(foreign.Id, null, 4, foreign.Version),
        ]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        mine.Position.Should().Be(1);
        foreign.Position.Should().Be(1);
    }

    [Fact]
    public async Task Reorder_ACategoryOfAnotherWorkspace_IsNotFound_SoNoChannelIsPlacedInsideIt()
    {
        AsCaller(_admin);
        var channel = Channel("a");
        var foreignGroup = Group("Foreign", 1, _otherWorkspaceId);

        var act = () => ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId,
            [new ChannelPlacement(channel.Id, foreignGroup.Id, 2, channel.Version)]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        channel.GroupId.Should().BeNull();
    }

    [Fact]
    public async Task Reorder_APrivateChannelTheAdminIsNotIn_IsNotFound_SoItsNameNeverLeaks()
    {
        AsCaller(_admin);
        var secret = Channel("secret", kind: SpaceKind.ChannelPrivate);

        var act = () => ReorderHandler().Handle(new ReorderChannelsCommand(_workspaceId,
            [new ChannelPlacement(secret.Id, null, 2, secret.Version)]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        secret.Position.Should().Be(1);
    }

    [Fact]
    public void Reorder_Validator_RefusesAnEmptyBatchMoreThanTwoHundredAndANonFinitePosition()
    {
        var validator = new ReorderChannelsCommandValidator();
        var one = new ChannelPlacement(Guid.NewGuid(), null, 1, 1);

        validator.Validate(new ReorderChannelsCommand(_workspaceId, [])).IsValid.Should().BeFalse();
        validator.Validate(new ReorderChannelsCommand(_workspaceId, Enumerable.Repeat(one, 201).ToList())).IsValid.Should().BeFalse();
        validator.Validate(new ReorderChannelsCommand(_workspaceId, [one with { Position = double.PositiveInfinity }])).IsValid.Should().BeFalse();
        validator.Validate(new ReorderChannelsCommand(_workspaceId, [one])).IsValid.Should().BeTrue();
    }
}
