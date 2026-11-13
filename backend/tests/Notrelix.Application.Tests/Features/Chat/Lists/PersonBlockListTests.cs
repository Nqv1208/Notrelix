using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Blocks.Queries.ListPersonBlocks;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Lists;

/// <summary>The caller's own blocks (UC-CHT-E06). A Guest has no Workspace directory (CHT-004), so a Guest sees only the blocks of people they share a space with.</summary>
public class PersonBlockListTests
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

    public PersonBlockListTests()
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

    private ListPersonBlocksQueryHandler BlocksHandler() => new(_db.Object, _context.Object, _members.Object);

    [Fact]
    public async Task The_block_list_is_the_callers_own_newest_first_and_never_another_persons()
    {
        AsUser(_member);
        _blocks.Add(PersonBlock.Create(_accountId, _workspaceId, _member, _colleague, Now.AddHours(-2)));
        _blocks.Add(PersonBlock.Create(_accountId, _workspaceId, _member, _stranger, Now.AddHours(-1)));
        _blocks.Add(PersonBlock.Create(_accountId, _workspaceId, _colleague, _member, Now));
        _blocks.Add(PersonBlock.Create(_accountId, _workspaceId, _owner, _stranger, Now));
        _blocks.Add(PersonBlock.Create(_accountId, Guid.NewGuid(), _member, _owner, Now));

        var result = await BlocksHandler().Handle(new ListPersonBlocksQuery(_workspaceId), CancellationToken.None);

        result.Data!.Select(b => b.UserId).Should().Equal(_stranger, _colleague);
        result.Data[0].BlockedAt.Should().Be(Now.AddHours(-1));
    }

    [Fact]
    public async Task A_person_who_blocked_the_caller_is_invisible_in_the_callers_list()
    {
        AsUser(_member);
        _blocks.Add(PersonBlock.Create(_accountId, _workspaceId, _colleague, _member, Now));

        var result = await BlocksHandler().Handle(new ListPersonBlocksQuery(_workspaceId), CancellationToken.None);

        result.Data.Should().BeEmpty("the blocked person is never told, and the list holds only what the caller did");
    }

    [Fact]
    public async Task A_guest_sees_only_the_blocks_of_people_they_share_a_space_with()
    {
        AsUser(_guest);
        Channel("shared", null, _owner, _guest, _colleague);
        Channel("elsewhere", null, _owner, _stranger);
        _blocks.Add(PersonBlock.Create(_accountId, _workspaceId, _guest, _colleague, Now.AddHours(-1)));
        _blocks.Add(PersonBlock.Create(_accountId, _workspaceId, _guest, _stranger, Now));

        var result = await BlocksHandler().Handle(new ListPersonBlocksQuery(_workspaceId), CancellationToken.None);

        result.Data!.Select(b => b.UserId).Should().Equal(_colleague);
    }
}
