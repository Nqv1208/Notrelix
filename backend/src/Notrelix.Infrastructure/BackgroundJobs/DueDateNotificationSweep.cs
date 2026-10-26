using Notrelix.Application.Common.SystemOperations;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Daily due-date notifications (accepted 2026-10-25): an assignee is told once that an open Item is due
/// tomorrow and once that it became overdue. "Tomorrow" and "overdue" are the calendar days of the
/// Workspace's own time zone. Bound from <c>Collaboration:DueDateNotifications</c> and validated on start.
/// See <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class DueDateNotificationOptions
{
    public const string SectionName = "Collaboration:DueDateNotifications";

    /// <summary>When false the job notifies nobody. Default true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Time between passes. Default 1 hour: every pass is idempotent per Item, assignee and due day, so a
    /// pass in the same local day adds nothing, and each Workspace gets its notifications in its own day.
    /// </summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Maximum Items read per batch. Default 500.</summary>
    public int BatchSize { get; init; } = 500;

    /// <summary>
    /// How many local days after its due day an Item can still be reported as overdue, so a day the job did
    /// not run is made up. Default 3.
    /// </summary>
    public int OverdueGraceDays { get; init; } = 3;
}

public sealed class DueDateNotificationOptionsValidator : IValidateOptions<DueDateNotificationOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 5_000;
    public const int MaxGraceDays = 14;

    public ValidateOptionsResult Validate(string? name, DueDateNotificationOptions options)
    {
        var failures = new List<string>();
        var section = DueDateNotificationOptions.SectionName;

        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.BatchSize is < 1 or > MaxBatchSize)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");
        if (options.OverdueGraceDays is < 1 or > MaxGraceDays)
            failures.Add($"{section}:OverdueGraceDays must be between 1 and {MaxGraceDays}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One pass: reads the open, unarchived Items due within the window in keyset batches (bounded by
/// <see cref="DueDateNotificationOptions.BatchSize"/>), and for each assignee creates one in-app notification
/// per Item and due day through the shared notification writer, which honours the member's own
/// <c>overdue</c> in-app preference and muted threads and is idempotent by its deduplication key
/// (<c>{type}:{itemId}:{dueDay}:{userId}</c>). It runs under the explicit system (worker) tenant context
/// with the RLS worker scope.
/// </summary>
internal sealed class DueDateNotificationSweeper
{
    private static readonly SystemOperationReason SweepReason =
        new("Collaboration", "Tell assignees about Items due tomorrow and overdue Items");

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _clock;
    private readonly ItemFollowerNotifier _notifier;
    private readonly DueDateNotificationOptions _options;
    private readonly ILogger<DueDateNotificationSweeper> _logger;

    public DueDateNotificationSweeper(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        IDateTimeProvider clock,
        ItemFollowerNotifier notifier,
        IOptions<DueDateNotificationOptions> options,
        ILogger<DueDateNotificationSweeper> logger)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _clock = clock;
        _notifier = notifier;
        _options = options.Value;
        _logger = logger;
    }

    private sealed record DueItem(
        Guid Id, Guid AccountId, Guid WorkspaceId, DateTimeOffset DueAt, IReadOnlyList<Guid> Assignees);

    /// <returns>The number of Items looked at.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var looked = 0;
        Guid? after = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var system = new SystemContextScope(_tenant, _logger, "due-date-notifications.sweep", SweepReason);
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _rls.ApplyAsync(cancellationToken);

            var batch = await ReadBatchAsync(now, after, cancellationToken);
            if (batch.Count > 0)
            {
                var zones = await ZonesAsync(batch.Select(i => i.WorkspaceId).Distinct().ToList(), cancellationToken);
                foreach (var item in batch)
                {
                    await NotifyAsync(item, zones.GetValueOrDefault(item.WorkspaceId, TimeZoneInfo.Utc), now, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            looked += batch.Count;
            if (batch.Count < _options.BatchSize) break;
            after = batch[^1].Id;
        }

        if (looked > 0)
            _logger.LogInformation("Due-date notification pass looked at {Count} items", looked);
        return looked;
    }

    private async Task<List<DueItem>> ReadBatchAsync(DateTimeOffset now, Guid? after, CancellationToken ct)
    {
        // The widest local day is 14 hours away from UTC, so this window covers "tomorrow" and the grace days
        // of every zone; the exact local day is decided per Workspace below.
        var from = now.AddDays(-(_options.OverdueGraceDays + 2));
        var to = now.AddDays(3);

        var items = await _context.BoardItems
            .AsNoTracking()
            .Where(i => i.DueAt != null && i.DueAt >= from && i.DueAt <= to
                        && i.CompletedAt == null
                        && i.DeletedAt == null
                        && !i.IsArchived
                        && (after == null || i.Id.CompareTo(after.Value) > 0))
            .OrderBy(i => i.Id)
            .Take(_options.BatchSize)
            .Select(i => new { i.Id, i.AccountId, i.WorkspaceId, DueAt = i.DueAt!.Value })
            .ToListAsync(ct);
        if (items.Count == 0) return [];

        var ids = items.Select(i => i.Id).ToList();
        var members = (await _context.BoardItemMembers
                .AsNoTracking()
                .Where(m => ids.Contains(m.ItemId))
                .Select(m => new { m.ItemId, m.UserId })
                .ToListAsync(ct))
            .GroupBy(m => m.ItemId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(m => m.UserId).Distinct().ToList());

        return items
            .Select(i => new DueItem(i.Id, i.AccountId, i.WorkspaceId, i.DueAt,
                members.GetValueOrDefault(i.Id) ?? []))
            .ToList();
    }

    private async Task<Dictionary<Guid, TimeZoneInfo>> ZonesAsync(IReadOnlyList<Guid> workspaceIds, CancellationToken ct)
    {
        var names = await _context.Workspaces
            .AsNoTracking()
            .Where(w => workspaceIds.Contains(w.Id))
            .Select(w => new { w.Id, w.Settings.TimeZone })
            .ToListAsync(ct);
        return names.ToDictionary(
            w => w.Id,
            w => TimeZoneInfo.TryFindSystemTimeZoneById(w.TimeZone ?? WorkspaceProfile.DefaultTimeZone, out var zone)
                ? zone
                : TimeZoneInfo.Utc);
    }

    private async Task NotifyAsync(DueItem item, TimeZoneInfo zone, DateTimeOffset now, CancellationToken ct)
    {
        if (item.Assignees.Count == 0) return;

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        var due = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(item.DueAt, zone).DateTime);

        string producedType;
        string title;
        if (due == today.AddDays(1))
        {
            producedType = NotificationTypes.ItemDueTomorrowProducedType;
            title = "An item assigned to you is due tomorrow";
        }
        else if (due < today && due >= today.AddDays(-_options.OverdueGraceDays))
        {
            producedType = NotificationTypes.ItemOverdueProducedType;
            title = "An item assigned to you is overdue";
        }
        else
        {
            return;
        }

        await _notifier.NotifyAsync(
            item.AccountId, item.WorkspaceId, item.Id, null, null,
            $"{item.Id:N}:{due:yyyy-MM-dd}", producedType, title, null, item.Assignees, now, ct);
    }
}

/// <summary>Hosted loop of <see cref="DueDateNotificationSweeper"/>; each pass runs in its own worker DI scope.</summary>
internal sealed class DueDateNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DueDateNotificationOptions _options;
    private readonly ILogger<DueDateNotificationService> _logger;

    public DueDateNotificationService(
        IServiceScopeFactory scopeFactory,
        IOptions<DueDateNotificationOptions> options,
        ILogger<DueDateNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("DueDateNotifications is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateWorkerAsyncScope();
                var sweeper = scope.ServiceProvider.GetRequiredService<DueDateNotificationSweeper>();
                await sweeper.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DueDateNotifications pass failed");
            }

            try
            {
                await Task.Delay(_options.Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
