using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Queries.AuthorizeChannelRealtimeSubscription;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Realtime;

/// <summary>UC-CHT-C01 and C06: the channel subscription is granted to an active member only; every other case is the same NotFound.</summary>
public class AuthorizeChannelRealtimeSubscriptionTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly List<Space> _spaces = [];
    private readonly List<Membership> _memberships = [];
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<IExecutionContextReader> _context = new();
    private Space _channel = null!;

    public AuthorizeChannelRealtimeSubscriptionTests()
    {
        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_memberships).Object);

        _members.Setup(m => m.FindAsync(_workspaceId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid ws, Guid user, CancellationToken _) => user == _outsider ? null : new WorkspaceMemberFact(user, "Member", true));

        _channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _owner, Now);
        _channel.AddMember(_member, MembershipRole.Member, Now);
        _spaces.Add(_channel);
        _memberships.AddRange(_channel.Memberships);

        AsUser(_member);
    }

    private void AsUser(Guid userId) => _context.Setup(c => c.Snapshot).Returns(new ExecutionContextSnapshot(
        userId, _accountId, _workspaceId, null, ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource, Guid.NewGuid().ToString("D")));

    private Task<Result<RealtimeSubscriptionGrant>> Authorize(Guid channelId) =>
        new AuthorizeChannelRealtimeSubscriptionQueryHandler(_context.Object, _db.Object, _members.Object)
            .Handle(new AuthorizeChannelRealtimeSubscriptionQuery(channelId), CancellationToken.None);

    [Fact]
    public async Task An_active_member_of_the_live_channel_is_granted_the_workspace_binding()
    {
        var result = await Authorize(_channel.Id);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().Be(new RealtimeSubscriptionGrant(_accountId, _workspaceId));
    }

    [Fact]
    public async Task A_non_member_of_a_public_channel_is_denied_as_a_missing_channel_is()
    {
        AsUser(_outsider);
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_outsider, "Member", true));

        var forbidden = await Assert.ThrowsAsync<NotFoundException>(() => Authorize(_channel.Id));
        var missing = await Assert.ThrowsAsync<NotFoundException>(() => Authorize(Guid.NewGuid()));

        // The gateway maps both to the same denial; the exception type is the same NotFound for both.
        missing.GetType().Should().Be(forbidden.GetType());
    }

    [Fact]
    public async Task A_person_who_left_the_channel_is_denied()
    {
        _channel.RemoveMember(_member, Now.AddMinutes(1));
        _memberships.Clear();
        _memberships.AddRange(_channel.Memberships);

        await Assert.ThrowsAsync<NotFoundException>(() => Authorize(_channel.Id));
    }

    [Fact]
    public async Task An_archived_channel_closes_for_its_members()
    {
        _channel.SetArchived(true, _owner, Now.AddMinutes(1));

        await Assert.ThrowsAsync<NotFoundException>(() => Authorize(_channel.Id));
    }

    [Fact]
    public async Task A_deleted_channel_is_denied()
    {
        _channel.Delete(_owner, Now.AddMinutes(1));

        await Assert.ThrowsAsync<NotFoundException>(() => Authorize(_channel.Id));
    }

    [Fact]
    public async Task A_caller_outside_the_workspace_is_denied()
    {
        AsUser(_outsider);
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>())).ReturnsAsync((WorkspaceMemberFact?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Authorize(_channel.Id));
    }
}
