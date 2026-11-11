using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Chat.Members.Services;
using Notrelix.Application.Features.Chat.Realtime;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Messaging.Consumers.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// workspace.member.removed against PostgreSQL: a person removed from a Workspace leaves every channel membership they held in
/// that Workspace, keeps the ones in another Workspace of the same account, is signalled once per channel, and no longer gets
/// unread frames for the removed Workspace's channels. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatWorkspaceExitIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Removed = new(2026, 11, 11, 8, 30, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _otherWorkspaceId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _reader = Guid.NewGuid();
    private readonly Guid _leaver = Guid.NewGuid();

    private Space _general = null!;
    private Space _ops = null!;
    private Space _otherGeneral = null!;

    public ChatWorkspaceExitIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        // Workspace A: general (public) and ops (private, the leaver is its only manager). Workspace B: general.
        _general = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _owner, Now);
        _general.AddMember(_reader, MembershipRole.Member, Now);
        _general.AddMember(_leaver, MembershipRole.Member, Now);

        _ops = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPrivate, "ops", null, null, 2, _leaver, Now);
        _ops.AddMember(_reader, MembershipRole.Member, Now);

        _otherGeneral = Space.CreateChannel(_accountId, _otherWorkspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _owner, Now);
        _otherGeneral.AddMember(_leaver, MembershipRole.Member, Now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ChatSpaces.AddRange(_general, _ops, _otherGeneral);
        seed.ChatMessages.AddRange(
            Message.CreateUserMessage(_accountId, _workspaceId, _general.Id, 1, _owner, "hello", "c-1", null, Now),
            Message.CreateUserMessage(_accountId, _otherWorkspaceId, _otherGeneral.Id, 1, _owner, "hi", "c-2", null, Now));
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private static ICurrentTenantContext WorkspaceTenant(Guid accountId, Guid workspaceId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, userId);
        return tenant;
    }

    [Fact]
    public async Task Removal_ends_the_memberships_in_that_workspace_and_keeps_the_other_workspace()
    {
        var access = new Mock<IAccessChangeSignal>();
        await ConsumeRemovalAsync(access.Object);

        await using var read = _db.CreateOwnerContext(SystemTenant());
        var memberships = await read.ChatMemberships.AsNoTracking().Where(m => m.UserId == _leaver).ToListAsync();

        memberships.Single(m => m.SpaceId == _general.Id).LeftAt.Should().Be(Removed);
        memberships.Single(m => m.SpaceId == _ops.Id).LeftAt.Should().Be(Removed, "the sole manager's channel is left without a manager, not kept");
        memberships.Single(m => m.SpaceId == _otherGeneral.Id).LeftAt.Should().BeNull("the other Workspace is untouched");

        access.Verify(a => a.ChannelPerson(_workspaceId, _general.Id, _leaver), Times.Once);
        access.Verify(a => a.ChannelPerson(_workspaceId, _ops.Id, _leaver), Times.Once);
        access.Verify(a => a.ChannelPerson(_otherWorkspaceId, It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Consuming_the_removal_again_changes_nothing_and_signals_nothing()
    {
        await ConsumeRemovalAsync(new Mock<IAccessChangeSignal>().Object);

        var second = new Mock<IAccessChangeSignal>();
        await ConsumeRemovalAsync(second.Object);

        second.VerifyNoOtherCalls();
        await using var read = _db.CreateOwnerContext(SystemTenant());
        (await read.ChatMemberships.AsNoTracking().Where(m => m.SpaceId == _general.Id && m.UserId == _leaver).SingleAsync())
            .LeftAt.Should().Be(Removed, "the first end time stands");
    }

    [Fact]
    public async Task After_removal_the_person_gets_no_unread_frame_for_that_workspace_but_still_does_for_the_other()
    {
        await ConsumeRemovalAsync(new Mock<IAccessChangeSignal>().Object);

        var published = new List<RealtimeResourceChangedV1>();
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var fanOut = new ChatUnreadFanOut(context, new CapturingPublisher(published), NullLogger<ChatUnreadFanOut>.Instance);

        await fanOut.NotifyCreatedAsync(NewMessage(_general.Id, _workspaceId, sequence: 2, "second"), Now, CancellationToken.None);
        var inWorkspace = published.Where(f => f.ChangeKind == ChatRealtime.UnreadChangedEvent).Select(f => f.ResourceId).ToList();
        inWorkspace.Should().BeEquivalentTo(new[] { _reader }, "the removed person is no longer a member of general");

        published.Clear();
        await fanOut.NotifyCreatedAsync(NewMessage(_otherGeneral.Id, _otherWorkspaceId, sequence: 2, "again"), Now, CancellationToken.None);
        var otherWorkspace = published.Where(f => f.ChangeKind == ChatRealtime.UnreadChangedEvent).Select(f => f.ResourceId).ToList();
        otherWorkspace.Should().BeEquivalentTo(new[] { _leaver }, "the person still belongs to the other Workspace's channel");
    }

    private async Task ConsumeRemovalAsync(IAccessChangeSignal access)
    {
        var message = new WorkspaceMemberRemovedIntegrationEvent(
            Guid.NewGuid(), _accountId, _workspaceId, _leaver, Guid.NewGuid(), ActorUserId: _owner, OccurredAt: Removed);

        await using var context = _db.CreateOwnerContext(WorkspaceTenant(_accountId, _workspaceId, _owner));
        var consumer = new ChatWorkspaceMemberRemovedConsumer(new ChatWorkspaceExit(context, access));
        var consume = new Mock<ConsumeContext<WorkspaceMemberRemovedIntegrationEvent>>();
        consume.SetupGet(c => c.Message).Returns(message);
        consume.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consume.Object);
        await context.SaveChangesAsync();
    }

    private ChatMessageCreatedIntegrationEvent NewMessage(Guid spaceId, Guid workspaceId, long sequence, string content) =>
        new(Guid.NewGuid(), _accountId, workspaceId, Guid.NewGuid(), spaceId, sequence, _owner, "user", content, $"c-{sequence}", 1,
            Guid.NewGuid(), ActorUserId: _owner, OccurredAt: Now);

    private sealed class CapturingPublisher(List<RealtimeResourceChangedV1> sink) : IRealtimePublisher
    {
        public Task PublishAsync(RealtimeResourceChangedV1 change, CancellationToken cancellationToken)
        {
            lock (sink) sink.Add(change);
            return Task.CompletedTask;
        }
    }
}
