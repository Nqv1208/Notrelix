using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Members.Services;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat;

/// <summary>
/// A person who leaves or is removed from a Workspace ends their active channel memberships in that Workspace only: set in one
/// read, idempotent, one access-changed signal per channel that changed. Other Workspaces and other people are untouched.
/// </summary>
public class ChatWorkspaceExitTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LeftAt = new(2026, 11, 11, 8, 0, 0, TimeSpan.Zero);

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _otherWorkspaceId = Guid.NewGuid();
    private readonly Guid _leaver = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();

    private readonly List<Space> _spaces = [];
    private readonly Mock<IAccessChangeSignal> _access = new();
    private readonly Mock<IChatDbContext> _db = new();

    private readonly Space _general;
    private readonly Space _ops;
    private readonly Space _alreadyLeft;
    private readonly Space _otherGeneral;
    private readonly Space _otherUnrelated;

    public ChatWorkspaceExitTests()
    {
        _general = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _owner, Now);
        _general.AddMember(_leaver, MembershipRole.Member, Now);
        _general.AddMember(_other, MembershipRole.Member, Now);

        // The leaver is the only manager of ops; the other person is a plain member, so the channel keeps a member.
        _ops = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPrivate, "ops", null, null, 2, _leaver, Now);
        _ops.AddMember(_other, MembershipRole.Member, Now);

        // The leaver left this channel before: its membership is already ended and must not change or signal.
        _alreadyLeft = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "old", null, null, 3, _owner, Now);
        _alreadyLeft.AddMember(_leaver, MembershipRole.Member, Now);
        _alreadyLeft.RemoveMember(_leaver, Now.AddDays(-1));

        // Another Workspace of the same account: the leaver stays in it.
        _otherGeneral = Space.CreateChannel(_accountId, _otherWorkspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _owner, Now);
        _otherGeneral.AddMember(_leaver, MembershipRole.Member, Now);

        // A channel of another Workspace that the leaver is not in at all.
        _otherUnrelated = Space.CreateChannel(_accountId, _otherWorkspaceId, SpaceKind.ChannelPublic, "random", null, null, 2, _owner, Now);
        _otherUnrelated.AddMember(_other, MembershipRole.Member, Now);

        _spaces.AddRange([_general, _ops, _alreadyLeft, _otherGeneral, _otherUnrelated]);
        _db.Setup(d => d.ChatMemberships).Returns(TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
    }

    private ChatWorkspaceExit Exit() => new(_db.Object, _access.Object);

    private static Membership MembershipOf(Space space, Guid userId) => space.Memberships.Single(m => m.UserId == userId);

    [Fact]
    public async Task Ends_the_person_s_active_memberships_in_that_workspace_only()
    {
        var ended = await Exit().EndMembershipsAsync(_workspaceId, _leaver, LeftAt, CancellationToken.None);

        ended.Should().Be(2, "general and ops end; the already-ended membership and the other Workspace do not count");
        MembershipOf(_general, _leaver).LeftAt.Should().Be(LeftAt);
        MembershipOf(_ops, _leaver).LeftAt.Should().Be(LeftAt);
        MembershipOf(_alreadyLeft, _leaver).LeftAt.Should().Be(Now.AddDays(-1), "an ended membership keeps its own end time");
        MembershipOf(_otherGeneral, _leaver).IsActive.Should().BeTrue("the person stays in the other Workspace");
        MembershipOf(_otherUnrelated, _other).IsActive.Should().BeTrue();
        MembershipOf(_general, _other).IsActive.Should().BeTrue("other people are never touched");
    }

    [Fact]
    public async Task Signals_access_changed_once_per_channel_that_changed()
    {
        await Exit().EndMembershipsAsync(_workspaceId, _leaver, LeftAt, CancellationToken.None);

        _access.Verify(a => a.ChannelPerson(_workspaceId, _general.Id, _leaver), Times.Once);
        _access.Verify(a => a.ChannelPerson(_workspaceId, _ops.Id, _leaver), Times.Once, "a sole manager leaving still closes the subscription");
        _access.Verify(a => a.ChannelPerson(It.IsAny<Guid>(), _alreadyLeft.Id, It.IsAny<Guid>()), Times.Never);
        _access.Verify(a => a.ChannelPerson(_otherWorkspaceId, It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _access.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Consuming_the_same_fact_twice_changes_nothing_the_second_time()
    {
        await Exit().EndMembershipsAsync(_workspaceId, _leaver, LeftAt, CancellationToken.None);
        var second = new Mock<IAccessChangeSignal>();
        var again = new ChatWorkspaceExit(_db.Object, second.Object);

        var ended = await again.EndMembershipsAsync(_workspaceId, _leaver, LeftAt.AddMinutes(5), CancellationToken.None);

        ended.Should().Be(0);
        second.VerifyNoOtherCalls();
        MembershipOf(_general, _leaver).LeftAt.Should().Be(LeftAt, "the first end time stands");
    }

    [Fact]
    public async Task A_person_with_no_channels_in_the_workspace_changes_nothing()
    {
        var stranger = Guid.NewGuid();

        var ended = await Exit().EndMembershipsAsync(_workspaceId, stranger, LeftAt, CancellationToken.None);

        ended.Should().Be(0);
        _access.VerifyNoOtherCalls();
    }
}
