using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Features.Chat.Public.Digest;
using Notrelix.Application.Features.Collaboration.Notifications.ChatDigest;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Configuration;
using Notrelix.Infrastructure.CrossContext.Collaboration.Notifications;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Notifications.ChatDigest;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Notifications;

/// <summary>
/// UC-CHT-H09: Collaboration's side of the missed-mention digest. Three missed mentions are one email, a repeated pass adds nothing, the
/// preference matrix decides (digest email on, the type's in-app on), read notifications and notifications outside the window are left out, what
/// Chat no longer allows is left out, the lines are capped with "and N more", and a person without an address gets nothing.
/// </summary>
public sealed class ChatDigestComposerTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Bao = Guid.NewGuid();
    private static readonly DateTimeOffset Sent = new(2026, 11, 13, 6, 0, 0, TimeSpan.Zero);
    private static readonly ChatDigestWindow Window = ChatDigestWindow.Latest(Sent, TimeSpan.FromHours(24));
    private static readonly DateTimeOffset InWindow = Window.Start.AddHours(10);

    private readonly ApplicationDbContext _context = CreateContext();
    private readonly Mock<IChatDigestFacts> _chat = new();
    private readonly Mock<IIdentityUserFacts> _identity = new();
    private readonly Mock<IMemberDeliveryAddresses> _addresses = new();
    private readonly Dictionary<Guid, string> _emails = new() { [Ana] = "ana@example.test", [Bao] = "bao@example.test" };
    private readonly List<Guid> _refused = [];

    public ChatDigestComposerTests()
    {
        _chat.Setup(c => c.DeliverableAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<ChatDigestCandidate>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid ws, Guid _, IReadOnlyCollection<ChatDigestCandidate> candidates, DateTimeOffset _, CancellationToken _) =>
                candidates.Where(c => !_refused.Contains(c.MessageId))
                    .Select(c => new ChatDigestFact(c.NotificationId, c.SpaceId, "general", c.MessageId, c.Reason, c.At, "hello " + c.MessageId.ToString("N")[..4], $"/workspaces/{ws}/chat/{c.SpaceId}"))
                    .ToList());
        _addresses.Setup(i => i.FindAsync(Workspace, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) => ids.Where(_emails.ContainsKey).ToDictionary(i => i, i => _emails[i]));
        _identity.Setup(i => i.FindLocaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync("en");
    }

    private ChatDigestComposer Composer() => new(
        _context, _chat.Object, _identity.Object, _addresses.Object,
        Microsoft.Extensions.Options.Options.Create(new FrontendOptions { AppBaseUrl = new Uri("https://app.test/") }),
        NullLogger<ChatDigestComposer>.Instance);

    private Task<ChatDigestRunResult> Run(int maxItems = 10, int batch = 50) =>
        Composer().RunAsync(Window, maxItems, batch, Sent, CancellationToken.None);

    /// <summary>Notifications are written through the real Chat intent path, at the given time.</summary>
    private async Task<Guid> Mention(Guid person, string reason = ChatNotificationReasons.Mention, DateTimeOffset? at = null, Guid? space = null)
    {
        var message = Guid.NewGuid();
        var source = new ChatNotificationSource(Account, Workspace, space ?? Guid.NewGuid(), message, null, Actor, Guid.NewGuid(), "general", "hello", at ?? InWindow);
        await new PostgresChatNotificationIntents(_context, NullLogger<PostgresChatNotificationIntents>.Instance)
            .DeliverAsync(source, [new ChatNotificationIntent(person, reason)], CancellationToken.None);
        return message;
    }

    private async Task DigestOn(Guid person, params NotificationPreferenceChange[] more)
    {
        var changes = new List<NotificationPreferenceChange> { new(NotificationTypes.Digest, NotificationChannels.Email, true) };
        changes.AddRange(more);
        _context.NotificationPreferenceSets.Add(NotificationPreferenceSet.Create(Account, Workspace, person, changes, Sent)!);
        FixQuietHours(_context);
        await _context.SaveChangesAsync();
    }

    private Task<List<EmailOutboxMessage>> Mails() => _context.EmailOutboxMessages.AsNoTracking().ToListAsync();

    [Fact]
    public async Task ThreeMissedMentionsAreOneDigestEmail()
    {
        await DigestOn(Ana);
        await Mention(Ana);
        await Mention(Ana, ChatNotificationReasons.DirectMessage);
        await Mention(Ana, ChatNotificationReasons.Broadcast);

        var result = await Run();

        result.Should().Be(new ChatDigestRunResult(1, 1, 0, 0, 0));
        var mail = (await Mails()).Should().ContainSingle().Subject;
        mail.RecipientEmail.Should().Be("ana@example.test");
        mail.RecipientUserId.Should().Be(Ana);
        mail.WorkspaceId.Should().Be(Workspace);
        mail.SourceContext.Should().Be("chat");
        mail.TemplateName.Should().Be("chat.digest");
        mail.Subject.Should().Be("You missed 3 chat messages");
        mail.BodyText.Should().Contain("https://app.test/workspaces/").And.Contain("/account/notifications");
        mail.DeduplicationKey.Should().Be(Window.DeduplicationKey(Workspace, Ana));
    }

    [Fact]
    public async Task ARepeatedPassQueuesNothingAgain_AndAnotherPersonStillGetsTheirOwn()
    {
        await DigestOn(Ana);
        await Mention(Ana);
        await Run();

        var again = await Run();
        await DigestOn(Bao);
        await Mention(Bao);
        var next = await Run();

        again.Should().Be(new ChatDigestRunResult(1, 0, 1, 0, 0));
        next.Queued.Should().Be(1);
        next.AlreadyQueued.Should().Be(1);
        (await Mails()).Should().HaveCount(2);
    }

    [Fact]
    public async Task AFailedSendIsRetriedByTheDispatcher_NotByAnotherQueuedMail()
    {
        await DigestOn(Ana);
        await Mention(Ana);
        await Run();
        var mail = await _context.EmailOutboxMessages.SingleAsync();
        mail.MarkProcessing("d", "t", Sent, 60);
        mail.MarkFailed("smtp", "down", Sent, "t");
        mail.ScheduleRetry(Sent.AddMinutes(5), Sent);
        await _context.SaveChangesAsync();

        await Run();

        (await Mails()).Should().ContainSingle("a failed send keeps its one row; the dispatcher retries it");
    }

    [Fact]
    public async Task APersonWhoDidNotTurnTheDigestOnGetsNothing()
    {
        await Mention(Ana);

        var result = await Run();

        result.Should().Be(new ChatDigestRunResult(1, 0, 0, 1, 0));
        (await Mails()).Should().BeEmpty();
    }

    [Fact]
    public async Task TheDigestCellOnEmailMustBeOn_AnotherTypesEmailCellIsNotEnough()
    {
        _context.NotificationPreferenceSets.Add(NotificationPreferenceSet.Create(
            Account, Workspace, Ana, [new NotificationPreferenceChange(NotificationTypes.Watching, NotificationChannels.Email, true)], Sent)!);
        FixQuietHours(_context);
        await _context.SaveChangesAsync();
        await Mention(Ana);

        await Run();

        (await Mails()).Should().BeEmpty();
    }

    [Fact]
    public async Task AnItemWhoseTypeIsSwitchedOffInAppAfterwardsIsLeftOut()
    {
        await DigestOn(Ana, new NotificationPreferenceChange(NotificationTypes.Watching, NotificationChannels.InApp, false));
        await Mention(Ana, ChatNotificationReasons.ThreadReply);
        await Mention(Ana, ChatNotificationReasons.Mention);

        await Run();

        (await Mails()).Single().Subject.Should().Be("You missed a chat message");
    }

    [Fact]
    public async Task ReadArchivedAndDismissedNotificationsAreLeftOut()
    {
        await DigestOn(Ana);
        await Mention(Ana);
        await Mention(Ana);
        await Mention(Ana);
        var recipients = await _context.NotificationRecipients.ToListAsync();
        recipients[0].MarkAsRead(Sent.AddHours(-1));
        recipients[1].Archive(Sent.AddHours(-1));
        await _context.SaveChangesAsync();

        await Run();

        (await Mails()).Single().Subject.Should().Be("You missed a chat message");
    }

    [Fact]
    public async Task OnlyNotificationsOfTheWindowAreReported()
    {
        await DigestOn(Ana);
        await Mention(Ana, at: Window.Start.AddMinutes(-1));
        await Mention(Ana, at: Window.End);

        var result = await Run();

        result.Queued.Should().Be(0);
        (await Mails()).Should().BeEmpty();
    }

    [Fact]
    public async Task WhatChatNoLongerAllowsIsLeftOut_AndWhenNothingIsLeftNoMailIsQueued()
    {
        await DigestOn(Ana);
        var muted = await Mention(Ana);
        var kept = await Mention(Ana);
        _refused.Add(muted);

        await Run();
        (await Mails()).Single().Subject.Should().Be("You missed a chat message");

        _refused.Add(kept);
        _context.EmailOutboxMessages.RemoveRange(await _context.EmailOutboxMessages.ToListAsync());
        await _context.SaveChangesAsync();
        var result = await Run();

        result.Nothing.Should().Be(1);
        (await Mails()).Should().BeEmpty();
    }

    [Fact]
    public async Task TheListIsCappedAtTheMostItemsWithAndNMore_NewestFirst()
    {
        await DigestOn(Ana);
        for (var i = 0; i < 5; i++)
            await Mention(Ana, at: InWindow.AddMinutes(i));

        await Run(maxItems: 2);

        var mail = (await Mails()).Single();
        mail.Subject.Should().Be("You missed 5 chat messages");
        mail.BodyText.Should().Contain("and 3 more");
        mail.BodyText!.Split("https://app.test/workspaces/").Length.Should().Be(4, "two item links and the settings link, plus the text before the first");
    }

    [Fact]
    public async Task APersonWithoutAnAddressGetsNothing()
    {
        _emails.Remove(Ana);
        await DigestOn(Ana);
        await Mention(Ana);

        var result = await Run();

        result.NoAddress.Should().Be(1);
        (await Mails()).Should().BeEmpty();
    }

    [Fact]
    public async Task DigestsAreGroupedPerPersonAcrossBatches()
    {
        await DigestOn(Ana);
        await DigestOn(Bao);
        await Mention(Ana);
        await Mention(Ana);
        await Mention(Bao);

        var result = await Run(batch: 1);

        result.Queued.Should().Be(2);
        (await Mails()).Select(m => m.RecipientUserId).Should().BeEquivalentTo([Ana, Bao]);
    }

    [Fact]
    public async Task TheOptionsAreValidated()
    {
        var validator = new Notrelix.Infrastructure.BackgroundJobs.ChatDigestOptionsValidator();

        validator.Validate(null, new Notrelix.Infrastructure.BackgroundJobs.ChatDigestOptions()).Succeeded.Should().BeTrue();
        validator.Validate(null, new Notrelix.Infrastructure.BackgroundJobs.ChatDigestOptions { MaxItems = 0 }).Failed.Should().BeTrue();
        validator.Validate(null, new Notrelix.Infrastructure.BackgroundJobs.ChatDigestOptions { Window = TimeSpan.FromSeconds(5) }).Failed.Should().BeTrue();
        validator.Validate(null, new Notrelix.Infrastructure.BackgroundJobs.ChatDigestOptions { Interval = TimeSpan.Zero }).Failed.Should().BeTrue();
        await Task.CompletedTask;
    }

    private static void FixQuietHours(ApplicationDbContext context)
    {
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
        return new DigestTestContext(options, tenant);
    }

    private sealed class DigestTestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }
}
