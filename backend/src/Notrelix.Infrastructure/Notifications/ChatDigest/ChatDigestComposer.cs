using Notrelix.Application.Features.Chat.Public.Digest;
using Notrelix.Application.Features.Collaboration.Notifications.ChatDigest;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Features.Identity.Public.DeliveryAddresses;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Configuration;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Notifications.ChatDigest;

/// <summary>What one pass of the digest did, for the log and the tests.</summary>
public sealed record ChatDigestRunResult(int People, int Queued, int AlreadyQueued, int Nothing, int NoAddress)
{
    public static readonly ChatDigestRunResult Empty = new(0, 0, 0, 0, 0);

    public ChatDigestRunResult Plus(ChatDigestRunResult other) =>
        new(People + other.People, Queued + other.Queued, AlreadyQueued + other.AlreadyQueued, Nothing + other.Nothing, NoAddress + other.NoAddress);
}

/// <summary>
/// The missed-mention digest (UC-CHT-H09, CHT-019): Collaboration's side. For one completed window it finds the people who still have unread
/// chat notifications created in it and whose preference matrix turns the digest on for email, asks Chat which of those notifications still
/// deserve a line now (<see cref="IChatDigestFacts"/>: membership, mute, level, read state, existence), and queues ONE email per person and
/// Workspace on the shared email outbox, the one mail path (<c>EmailDispatcher</c> sends it, retries a failure and never queues it again).
/// The email carries at most <c>maxItems</c> lines, newest first, and "and N more" for the rest. A repeated pass adds nothing: the outbox row's
/// deduplication key is <c>chat-digest:{workspace}:{user}:{window}</c> and is unique. Chat never sees an address and Collaboration never reads
/// a Chat table.
/// </summary>
/// <remarks>
/// A person's preference is the <c>digest</c> type on the <c>email</c> channel (off by default: the person opts in); a notification also
/// needs its own type's in-app cell on (a chat mention is <c>mention</c>, a thread reply is <c>watching</c>). The matrix has no frequency, so
/// the cadence is the window length of the job. The window is the unit of "one digest": a notification created in a window is reported in
/// that window's digest or not at all (a later window does not repeat it), so a person who does not read for a week gets one digest per day
/// of new items, not the same items again. The address comes from Identity's system-sender fact (<see cref="IMemberDeliveryAddresses"/>), which a job can use where the request-bound address disclosure cannot; a Guest has none and receives no digest.
/// </remarks>
public sealed class ChatDigestComposer(
    ApplicationDbContext context,
    IChatDigestFacts chat,
    IIdentityUserFacts identity,
    IMemberDeliveryAddresses addresses,
    IOptions<FrontendOptions> frontend,
    ILogger<ChatDigestComposer> logger)
{
    public const string SourceContext = "chat";
    public const string TemplateKey = "chat.digest";

    private static readonly string[] ChatTypes =
    [
        NotificationTypes.ChatMentionProducedType,
        NotificationTypes.ChatDirectMessageProducedType,
        NotificationTypes.ChatThreadReplyProducedType,
        NotificationTypes.ChatBroadcastProducedType,
    ];

    private sealed record Person(Guid AccountId, Guid WorkspaceId, Guid UserId);

    private sealed record Pending(Guid NotificationId, Guid SpaceId, Guid MessageId, string Reason, string Type, DateTimeOffset At);

    public async Task<ChatDigestRunResult> RunAsync(
        ChatDigestWindow window, int maxItems, int batchSize, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var total = ChatDigestRunResult.Empty;
        var skip = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var people = await PeopleAsync(window, skip, batchSize, now, cancellationToken);
            if (people.Count == 0)
                break;

            total = total.Plus(await RunBatchAsync(people, window, maxItems, now, cancellationToken));
            skip += people.Count;
            if (people.Count < batchSize)
                break;
        }

        return total;
    }

    private async Task<IReadOnlyList<Person>> PeopleAsync(
        ChatDigestWindow window, int skip, int take, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rows = await UnreadInWindow(window, now)
            .Select(x => new { x.AccountId, x.WorkspaceId, x.UserId })
            .Distinct()
            .OrderBy(x => x.WorkspaceId)
            .ThenBy(x => x.UserId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return rows.Select(r => new Person(r.AccountId, r.WorkspaceId, r.UserId)).ToList();
    }

    private IQueryable<UnreadRow> UnreadInWindow(ChatDigestWindow window, DateTimeOffset now) =>
        from item in context.NotificationItems.AsNoTracking()
        join recipient in context.NotificationRecipients.AsNoTracking() on item.Id equals recipient.NotificationId
        where item.SourceContext == SourceContext
              && ChatTypes.Contains(item.NotificationType)
              && item.Status == NotificationItemStatus.Active
              && item.DeletedAt == null
              && item.CreatedAt >= window.Start && item.CreatedAt < window.End
              && item.ResourceId != null && item.SubjectId != null
              && recipient.ReadAt == null && recipient.DoneAt == null && recipient.ArchivedAt == null && recipient.DismissedAt == null
              && (recipient.SnoozedUntil == null || recipient.SnoozedUntil <= now)
        select new UnreadRow
        {
            ItemId = item.Id,
            AccountId = item.AccountId,
            WorkspaceId = item.WorkspaceId,
            UserId = recipient.RecipientUserId,
            SpaceId = item.ResourceId!.Value,
            MessageId = item.SubjectId!.Value,
            Type = item.NotificationType,
            CreatedAt = item.CreatedAt,
        };

    private sealed class UnreadRow
    {
        public Guid ItemId { get; init; }
        public Guid AccountId { get; init; }
        public Guid WorkspaceId { get; init; }
        public Guid UserId { get; init; }
        public Guid SpaceId { get; init; }
        public Guid MessageId { get; init; }
        public string Type { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
    }

    private async Task<ChatDigestRunResult> RunBatchAsync(
        IReadOnlyList<Person> people, ChatDigestWindow window, int maxItems, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var userIds = people.Select(p => p.UserId).Distinct().ToList();
        var workspaceIds = people.Select(p => p.WorkspaceId).Distinct().ToList();

        var sets = await context.NotificationPreferenceSets.AsNoTracking()
            .Include(s => s.Entries)
            .Where(s => workspaceIds.Contains(s.WorkspaceId) && userIds.Contains(s.UserId))
            .ToListAsync(cancellationToken);

        var keys = people.ToDictionary(p => p, p => window.DeduplicationKey(p.WorkspaceId, p.UserId));
        var keyList = keys.Values.ToList();
        var queued = (await context.EmailOutboxMessages.AsNoTracking()
                .Where(m => keyList.Contains(m.DeduplicationKey))
                .Select(m => m.DeduplicationKey)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var result = new ChatDigestRunResult(people.Count, 0, 0, 0, 0);
        foreach (var person in people)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (queued.Contains(keys[person]))
            {
                result = result with { AlreadyQueued = result.AlreadyQueued + 1 };
                continue;
            }

            var set = sets.FirstOrDefault(s => s.WorkspaceId == person.WorkspaceId && s.UserId == person.UserId);
            var digestOn = set?.IsEnabled(NotificationTypes.Digest, NotificationChannels.Email)
                ?? NotificationPreferenceDefaults.IsEnabled(NotificationTypes.Digest, NotificationChannels.Email);
            if (!digestOn)
            {
                result = result with { Nothing = result.Nothing + 1 };
                continue;
            }

            var outcome = await ComposeForAsync(person, set, window, keys[person], maxItems, now, cancellationToken);
            result = outcome switch
            {
                Outcome.Queued => result with { Queued = result.Queued + 1 },
                Outcome.Duplicate => result with { AlreadyQueued = result.AlreadyQueued + 1 },
                Outcome.NoAddress => result with { NoAddress = result.NoAddress + 1 },
                _ => result with { Nothing = result.Nothing + 1 },
            };
        }

        return result;
    }

    private enum Outcome { Nothing, Queued, Duplicate, NoAddress }

    private async Task<Outcome> ComposeForAsync(
        Person person, NotificationPreferenceSet? set, ChatDigestWindow window, string key, int maxItems, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rows = await UnreadInWindow(window, now)
            .Where(x => x.WorkspaceId == person.WorkspaceId && x.UserId == person.UserId)
            .ToListAsync(cancellationToken);

        var pending = rows
            .Select(r => new Pending(r.ItemId, r.SpaceId, r.MessageId, ReasonOf(r.Type), r.Type, r.CreatedAt))
            .Where(p => TypeIsOn(set, p.Type))
            .ToList();
        if (pending.Count == 0)
            return Outcome.Nothing;

        var facts = await chat.DeliverableAsync(
            person.WorkspaceId,
            person.UserId,
            pending.Select(p => new ChatDigestCandidate(p.NotificationId, p.SpaceId, p.MessageId, p.Reason, p.At)).ToList(),
            now,
            cancellationToken);
        if (facts.Count == 0)
            return Outcome.Nothing;

        var emails = await addresses.FindAsync(person.WorkspaceId, [person.UserId], cancellationToken);
        if (!emails.TryGetValue(person.UserId, out var email))
            return Outcome.NoAddress;

        var ordered = facts.OrderByDescending(f => f.At).ThenBy(f => f.MessageId).ToList();
        var baseUrl = frontend.Value.AppBaseUrl.ToString().TrimEnd('/');
        var lines = ordered.Take(Math.Max(1, maxItems))
            .Select(f => new ChatDigestLine(f.SpaceName, f.Reason, f.Preview, baseUrl + f.Path))
            .ToList();
        var rendered = ChatDigestMessage.Render(
            await identity.FindLocaleAsync(person.UserId, cancellationToken),
            lines,
            ordered.Count - lines.Count,
            $"{baseUrl}/workspaces/{person.WorkspaceId}/account/notifications");

        context.EmailOutboxMessages.Add(EmailOutboxMessage.CreateRendered(
            new QueueRenderedEmailRequest(
                DeduplicationKey: key,
                RecipientEmail: email,
                RecipientName: null,
                Subject: rendered.Subject,
                BodyHtml: rendered.BodyHtml,
                BodyText: rendered.BodyText,
                WorkspaceId: person.WorkspaceId,
                RecipientUserId: person.UserId,
                SourceContext: SourceContext,
                TemplateKey: TemplateKey,
                Priority: 200),
            now));

        // A unique violation aborts a PostgreSQL transaction, so each person is saved behind a savepoint the pass can return to.
        var transaction = context.Database.CurrentTransaction;
        if (transaction is not null)
            await transaction.CreateSavepointAsync("chat_digest_person", cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Outcome.Queued;
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex))
        {
            // Another instance queued this person's digest first; the unique key is the guarantee.
            if (transaction is not null)
                await transaction.RollbackToSavepointAsync("chat_digest_person", cancellationToken);
            logger.LogDebug("Chat digest of Workspace {WorkspaceId} for window {Window} was queued by another instance", person.WorkspaceId, window.Key);
            context.ChangeTracker.Clear();
            return Outcome.Duplicate;
        }
    }

    private static bool TypeIsOn(NotificationPreferenceSet? set, string producedType)
    {
        var type = NotificationTypes.FromProducedType(producedType);
        if (type is null)
            return false;
        return set?.IsEnabled(type, NotificationChannels.InApp) ?? NotificationPreferenceDefaults.IsEnabled(type, NotificationChannels.InApp);
    }

    internal static string ReasonOf(string producedType) => producedType switch
    {
        NotificationTypes.ChatMentionProducedType => ChatNotificationReasons.Mention,
        NotificationTypes.ChatDirectMessageProducedType => ChatNotificationReasons.DirectMessage,
        NotificationTypes.ChatThreadReplyProducedType => ChatNotificationReasons.ThreadReply,
        _ => ChatNotificationReasons.Broadcast,
    };

    private static bool IsDuplicate(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            return true;

        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("23505", StringComparison.Ordinal);
    }
}
