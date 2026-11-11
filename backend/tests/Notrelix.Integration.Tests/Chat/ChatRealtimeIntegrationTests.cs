using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Messaging.Consumers.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-C09 against PostgreSQL: a new message's per-user unread frames reach every active, unmuted member except the author,
/// each with that person's own count, and the frames carry no content. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatRealtimeIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _reader = Guid.NewGuid();
    private readonly Guid _muted = Guid.NewGuid();
    private readonly Guid _left = Guid.NewGuid();

    private Space _space = null!;

    public ChatRealtimeIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        // general (public): owner (author), reader, muted (mute on), left (left the space, so no membership row counts).
        _space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _owner, Now);
        _space.AddMember(_reader, MembershipRole.Member, Now);
        _space.AddMember(_muted, MembershipRole.Member, Now);
        _space.AddMember(_left, MembershipRole.Member, Now);
        _space.Memberships.Single(m => m.UserId == _muted).SetNotificationLevel(NotificationLevel.None, null, Now);
        _space.RemoveMember(_left, Now);

        var messages = new List<Message>
        {
            Message.CreateUserMessage(_accountId, _workspaceId, _space.Id, 1, _owner, "one", "c-1", null, Now),
            Message.CreateUserMessage(_accountId, _workspaceId, _space.Id, 2, _reader, "two", "c-2", null, Now),
            Message.CreateUserMessage(_accountId, _workspaceId, _space.Id, 3, _owner, "three", "c-3", null, Now),
        };
        messages[1].Delete(_reader, Now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ChatSpaces.Add(_space);
        seed.ChatMessages.AddRange(messages);
        await seed.SaveChangesAsync();
        await using var set = _db.CreateOwnerContext(SystemTenant());
        await set.Database.ExecuteSqlInterpolatedAsync($"UPDATE chat.spaces SET last_sequence = 3 WHERE id = {_space.Id}");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    [Fact]
    public async Task A_new_message_publishes_one_unread_frame_per_unmuted_member_except_the_author()
    {
        var published = new List<RealtimeResourceChangedV1>();
        var publisher = new CapturingPublisher(published);
        var message = new ChatMessageCreatedIntegrationEvent(
            Guid.NewGuid(), _accountId, _workspaceId, Guid.NewGuid(), _space.Id, 3, _owner, "user", "three", "c-3", 1,
            Guid.NewGuid(), ActorUserId: _owner, OccurredAt: Now);

        await using var context = _db.CreateOwnerContext(SystemTenant());
        var fanOut = new ChatUnreadFanOut(context, publisher, NullLogger<ChatUnreadFanOut>.Instance);
        await fanOut.NotifyCreatedAsync(message, Now, CancellationToken.None);

        var frames = published.Where(f => f.ChangeKind == ChatRealtime.UnreadChangedEvent).ToList();
        frames.Select(f => f.ResourceId).Should().BeEquivalentTo(new[] { _reader }, "the author, the muted member and the member who left get none");

        var reader = frames.Single();
        reader.TopicNamespace.Should().Be("user");
        reader.StreamKey.Should().Be($"user:notifications:{_reader:N}");
        reader.PayloadContract.Should().Be("chat.unread.v1");
        // The reader has not read anything: message 1 (owner) and message 3 (owner) are unread; message 2 is deleted.
        reader.Payload.GetProperty("spaceId").GetGuid().Should().Be(_space.Id);
        reader.Payload.GetProperty("unread").GetInt32().Should().Be(2);
        reader.Payload.GetProperty("mentions").GetInt32().Should().Be(0);
        reader.Payload.ToString().Should().NotContain("three", "the per-user frame carries no message content");
    }

    private sealed class CapturingPublisher(List<RealtimeResourceChangedV1> sink) : IRealtimePublisher
    {
        public Task PublishAsync(RealtimeResourceChangedV1 change, CancellationToken cancellationToken)
        {
            lock (sink) sink.Add(change);
            return Task.CompletedTask;
        }
    }
}
