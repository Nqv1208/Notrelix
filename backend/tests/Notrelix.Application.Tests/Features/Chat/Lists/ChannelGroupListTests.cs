using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.ChannelGroups.Queries.ListChannelGroups;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Application.Tests.Features.Collaboration;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Lists;

/// <summary>The category names the sidebar shows (UC-CHT-A02, A04). A Guest has no Workspace directory (CHT-004), so a Guest reads only the categories that hold a channel they are in.</summary>
public class ChannelGroupListTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _colleague = Guid.NewGuid();
    private readonly Guid _stranger = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<ChannelGroup> _groups = [];
    private readonly List<PersonBlock> _blocks = [];
    private readonly Mock<IChatDbContext> _db = new();

    public ChannelGroupListTests()
    {
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);

        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(() => TestDbSet.Create(_groups).Object);
        _db.Setup(d => d.ChatBlocks).Returns(() => TestDbSet.Create(_blocks).Object);

        foreach (var (id, role) in new[] { (_owner, "Owner"), (_member, "Member"), (_colleague, "Member"), (_stranger, "Member"), (_guest, "Guest") })
            _members.Setup(m => m.FindAsync(_workspaceId, id, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(id, role, true));
    }

    private void AsUser(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private ChannelGroup Group(string name, double position)
    {
        var group = ChannelGroup.Create(_accountId, _workspaceId, name, position, _owner, Now);
        _groups.Add(group);
        return group;
    }

    private Space Channel(string name, ChannelGroup? group, params Guid[] members)
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, name, null, group?.Id, 1, _owner, Now);
        foreach (var user in members) space.AddMember(user, MembershipRole.Member, Now);
        _spaces.Add(space);
        return space;
    }

    private ListChannelGroupsQueryHandler GroupsHandler() => new(_db.Object, _context.Object, _members.Object);

    [Fact]
    public async Task A_member_reads_every_category_name_in_sidebar_order()
    {
        AsUser(_member);
        Group("Projects", 2);
        Group("Company", 1);
        Group("Social", 1);

        var result = await GroupsHandler().Handle(new ListChannelGroupsQuery(_workspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Select(g => g.Name).Should().Equal("Company", "Social", "Projects");
        result.Data.Should().OnlyContain(g => g.Version >= 1);
    }

    [Fact]
    public async Task Categories_of_another_workspace_are_not_listed()
    {
        AsUser(_member);
        Group("Mine", 1);
        _groups.Add(ChannelGroup.Create(_accountId, Guid.NewGuid(), "Elsewhere", 1, _owner, Now));

        var result = await GroupsHandler().Handle(new ListChannelGroupsQuery(_workspaceId), CancellationToken.None);

        result.Data!.Select(g => g.Name).Should().Equal("Mine");
    }

    [Fact]
    public async Task A_guest_reads_only_the_categories_that_hold_a_channel_they_are_in()
    {
        AsUser(_guest);
        var shown = Group("Clients", 1);
        var hiddenByMembership = Group("Internal", 2);
        Group("Empty", 3);
        Channel("client-room", shown, _owner, _guest);
        Channel("staff-room", hiddenByMembership, _owner, _member);

        var result = await GroupsHandler().Handle(new ListChannelGroupsQuery(_workspaceId), CancellationToken.None);

        result.Data!.Select(g => g.Name).Should().Equal("Clients");
    }
}
