using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Features.Collaboration.Notifications;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.CrossContext.Collaboration.Notifications;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>
/// UC-CHT-G09: Collaboration's side of the Chat notification intent boundary. One notification per message, recipient and reason (a repeat of
/// the fact adds nothing), never the actor, the recipient's own preference and the mute of the space win, and the preview is the short excerpt.
/// </summary>
public sealed class ChatNotificationIntentsTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Space = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Bao = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private static ChatNotificationSource Source(Guid? message = null, string? preview = "hello") =>
        new(Account, Workspace, Space, message ?? Guid.NewGuid(), null, Actor, Guid.NewGuid(), "general", preview, Now);

    private static PostgresChatNotificationIntents Intents(ApplicationDbContext context) =>
        new(context, NullLogger<PostgresChatNotificationIntents>.Instance);

    [Fact]
    public async Task EachReasonWritesOneInboxNotificationOfItsOwnTypeAgainstTheSpace()
    {
        var context = CreateContext();
        var source = Source();

        var delivery = await Intents(context).DeliverAsync(source,
        [
            new ChatNotificationIntent(Ana, ChatNotificationReasons.Mention),
            new ChatNotificationIntent(Bao, ChatNotificationReasons.DirectMessage),
        ], CancellationToken.None);

        delivery.Should().Be(new ChatNotificationDelivery(2, 0, 0));
        var items = await context.NotificationItems.ToListAsync();
        items.Select(i => i.NotificationType).Should().BeEquivalentTo(NotificationTypes.ChatMentionProducedType, NotificationTypes.ChatDirectMessageProducedType);
        items.Should().OnlyContain(i => i.ResourceKind == NotificationTypes.ChatSpaceResourceKind && i.ResourceId == Space
                                        && i.ActorUserId == Actor && i.SubjectId == source.MessageId && i.Body == "hello");
        (await context.NotificationRecipients.Select(r => r.RecipientUserId).ToListAsync()).Should().BeEquivalentTo([Ana, Bao]);
    }

    [Fact]
    public async Task TheSameFactDeliveredTwiceWritesOneNotification()
    {
        var context = CreateContext();
        var source = Source();
        var intents = new[] { new ChatNotificationIntent(Ana, ChatNotificationReasons.Mention) };

        await Intents(context).DeliverAsync(source, intents, CancellationToken.None);
        var again = await Intents(context).DeliverAsync(source, intents, CancellationToken.None);

        again.Should().Be(new ChatNotificationDelivery(0, 1, 0));
        (await context.NotificationItems.CountAsync()).Should().Be(1);
        (await context.NotificationRecipients.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ADifferentReasonForTheSameMessageAndPersonIsAnotherFact_AndADuplicateIntentInOneCallCountsOnce()
    {
        var context = CreateContext();
        var source = Source();

        await Intents(context).DeliverAsync(source,
        [
            new ChatNotificationIntent(Ana, ChatNotificationReasons.Mention),
            new ChatNotificationIntent(Ana, ChatNotificationReasons.Mention),
            new ChatNotificationIntent(Ana, ChatNotificationReasons.Broadcast),
        ], CancellationToken.None);

        (await context.NotificationItems.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task TheActorAndAnUnknownReasonAreNeverDelivered()
    {
        var context = CreateContext();

        var delivery = await Intents(context).DeliverAsync(Source(),
        [
            new ChatNotificationIntent(Actor, ChatNotificationReasons.Mention),
            new ChatNotificationIntent(Ana, "gossip"),
            new ChatNotificationIntent(Guid.Empty, ChatNotificationReasons.Mention),
        ], CancellationToken.None);

        delivery.Should().Be(ChatNotificationDelivery.None);
        context.NotificationItems.Should().BeEmpty();
    }

    [Fact]
    public async Task APersonWhoSwitchedTheMentionCellOffGetsNoMentionLikeNotification_ButStillGetsThreadReplies()
    {
        var context = CreateContext();
        context.NotificationPreferenceSets.Add(NotificationPreferenceSet.Create(
            Account, Workspace, Ana, [new NotificationPreferenceChange(NotificationTypes.Mention, NotificationChannels.InApp, false)], Now)!);
        FixQuietHours(context);
        await context.SaveChangesAsync();

        var delivery = await Intents(context).DeliverAsync(Source(),
        [
            new ChatNotificationIntent(Ana, ChatNotificationReasons.Mention),
            new ChatNotificationIntent(Ana, ChatNotificationReasons.DirectMessage),
            new ChatNotificationIntent(Ana, ChatNotificationReasons.Broadcast),
            new ChatNotificationIntent(Ana, ChatNotificationReasons.ThreadReply),
        ], CancellationToken.None);

        delivery.Should().Be(new ChatNotificationDelivery(1, 0, 3));
        (await context.NotificationItems.SingleAsync()).NotificationType.Should().Be(NotificationTypes.ChatThreadReplyProducedType);
    }

    [Fact]
    public async Task TheThreadMuteOfTheSpaceSilencesTheRecipientOnly()
    {
        var context = CreateContext();
        context.NotificationThreadMutes.Add(NotificationThreadMuteRecord.Create(Account, Workspace, Ana, NotificationTypes.ChatSpaceResourceKind, Space, Now));
        await context.SaveChangesAsync();

        var delivery = await Intents(context).DeliverAsync(Source(),
        [
            new ChatNotificationIntent(Ana, ChatNotificationReasons.Mention),
            new ChatNotificationIntent(Bao, ChatNotificationReasons.Mention),
        ], CancellationToken.None);

        delivery.Should().Be(new ChatNotificationDelivery(1, 0, 1));
        (await context.NotificationRecipients.SingleAsync()).RecipientUserId.Should().Be(Bao);
    }

    [Fact]
    public async Task TheBodyIsTheShortPlainExcerptNeverTheWholeMessage()
    {
        var context = CreateContext();

        await Intents(context).DeliverAsync(Source(preview: "**bold** [a link](https://x.test) " + new string('z', 500)),
            [new ChatNotificationIntent(Ana, ChatNotificationReasons.Mention)], CancellationToken.None);

        var body = (await context.NotificationItems.SingleAsync()).Body!;
        body.Length.Should().BeLessThanOrEqualTo(NotificationExcerpt.Length);
        body.Should().StartWith("bold a link zzz").And.NotContain("**").And.NotContain("https://");
    }

    [Fact]
    public async Task AnEmptyMessageGivesANotificationWithoutAPreview()
    {
        var context = CreateContext();

        await Intents(context).DeliverAsync(Source(preview: "   "), [new ChatNotificationIntent(Ana, ChatNotificationReasons.Mention)], CancellationToken.None);

        (await context.NotificationItems.SingleAsync()).Body.Should().BeNull();
    }

    private static void FixQuietHours(ApplicationDbContext context)
    {
        // Postgres fills quiet_hours_json from its '{}' column default; the in-memory store needs the value set.
        foreach (var tracked in context.ChangeTracker.Entries())
        {
            if (tracked.Metadata.FindProperty("QuietHoursJson") is not null)
                tracked.Property("QuietHoursJson").CurrentValue = JsonDocument.Parse("{}");
        }
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return new IntentsTestContext(options, tenant);
    }

    private sealed class IntentsTestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }
}
