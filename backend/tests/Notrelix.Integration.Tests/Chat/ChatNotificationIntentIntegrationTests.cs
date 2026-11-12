using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Features.Chat.Notifications.Services;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.CrossContext.Collaboration.Notifications;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-G09 against PostgreSQL: a mention, a direct message and a broadcast reach the Inbox once through Collaboration's own
/// notification rows and the unique deduplication index; a person who left before delivery, a muted member and the author get nothing.
/// Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatNotificationIntentIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _author = Guid.NewGuid();
    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _bao = Guid.NewGuid();
    private readonly Guid _chi = Guid.NewGuid();

    private Space _channel = null!;
    private Space _direct = null!;
    private Message _mention = null!;
    private Message _broadcast = null!;
    private Message _dm = null!;

    public ChatNotificationIntentIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

        _channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _author, Now);
        _channel.AddMember(_ana, MembershipRole.Member, Now);
        _channel.AddMember(_bao, MembershipRole.Member, Now);
        _channel.AddMember(_chi, MembershipRole.Member, Now);
        _channel.Memberships.Single(m => m.UserId == _chi).SetNotificationLevel(NotificationLevel.None, null, Now);
        _direct = Space.CreateDirect(_accountId, _workspaceId, _author, _ana, Now);

        _mention = Message.CreateUserMessage(_accountId, _workspaceId, _channel.Id, 1, _author, "hi @Ana", "c-1", null, Now);
        _broadcast = Message.CreateUserMessage(_accountId, _workspaceId, _channel.Id, 2, _author, "@channel standup", "c-2", null, Now);
        _dm = Message.CreateUserMessage(_accountId, _workspaceId, _direct.Id, 1, _author, "ping", "c-3", null, Now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ChatSpaces.AddRange(_channel, _direct);
        seed.ChatMessages.AddRange(_mention, _broadcast, _dm);
        seed.ChatMessageMentions.AddRange(
            MessageMention.Create(_accountId, _workspaceId, _channel.Id, _mention.Id, new MentionSpan(MentionKind.User, 3, 4, _ana)),
            MessageMention.Create(_accountId, _workspaceId, _channel.Id, _broadcast.Id, new MentionSpan(MentionKind.BroadcastChannel, 0, 8, null)));
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private FakeCurrentTenantContext WorkspaceTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_accountId, _workspaceId, _author);
        return tenant;
    }

    private async Task<ChatNotificationDelivery> HandOffAsync(Message message)
    {
        await using var context = _db.CreateOwnerContext(WorkspaceTenant());
        var handoff = new ChatNotificationHandoff(
            context, new PostgresChatNotificationIntents(context, NullLogger<PostgresChatNotificationIntents>.Instance),
            NullLogger<ChatNotificationHandoff>.Instance);
        return await handoff.HandOffAsync(_workspaceId, message.Id, Guid.NewGuid(), Now, CancellationToken.None);
    }

    private async Task<List<(Guid Recipient, string Type)>> InboxAsync()
    {
        await using var read = _db.CreateOwnerContext(SystemTenant());
        return (await (from r in read.NotificationRecipients.AsNoTracking()
                       join i in read.NotificationItems.AsNoTracking() on r.NotificationId equals i.Id
                       orderby i.CreatedAt, i.NotificationType, r.RecipientUserId
                       select new { r.RecipientUserId, i.NotificationType }).ToListAsync())
            .Select(x => (x.RecipientUserId, x.NotificationType)).ToList();
    }

    [Fact]
    public async Task A_mention_a_direct_message_and_a_broadcast_reach_the_inbox_once_each()
    {
        (await HandOffAsync(_mention)).Delivered.Should().Be(1);
        (await HandOffAsync(_dm)).Delivered.Should().Be(1);
        (await HandOffAsync(_broadcast)).Delivered.Should().Be(2, "the muted member and the author are left out");

        var inbox = await InboxAsync();
        inbox.Should().BeEquivalentTo(new (Guid, string)[]
        {
            (_ana, NotificationTypes.ChatMentionProducedType),
            (_ana, NotificationTypes.ChatDirectMessageProducedType),
            (_ana, NotificationTypes.ChatBroadcastProducedType),
            (_bao, NotificationTypes.ChatBroadcastProducedType),
        });
    }

    [Fact]
    public async Task A_redelivered_fact_adds_nothing_because_the_deduplication_index_holds()
    {
        await HandOffAsync(_mention);
        var again = await HandOffAsync(_mention);

        again.Should().Be(new ChatNotificationDelivery(0, 1, 0));
        (await InboxAsync()).Should().HaveCount(1);
    }

    [Fact]
    public async Task A_person_who_left_the_channel_before_delivery_gets_no_notification()
    {
        await using (var leave = _db.CreateOwnerContext(SystemTenant()))
        {
            var membership = await leave.ChatMemberships.SingleAsync(m => m.SpaceId == _channel.Id && m.UserId == _ana);
            membership.EndWorkspaceExit(Now.AddMinutes(1));
            await leave.SaveChangesAsync();
        }

        (await HandOffAsync(_mention)).Should().Be(ChatNotificationDelivery.None);

        (await InboxAsync()).Should().BeEmpty();
    }
}
