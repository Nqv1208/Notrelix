using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Exports;

/// <summary>Where an export is in its life (UC-CHT-H06).</summary>
public enum ChatExportStatus
{
    Queued,
    Running,
    Ready,
    Failed,
    Expired,
}

public static class ChatExportStatusWire
{
    public static string ToWire(this ChatExportStatus status) => status switch
    {
        ChatExportStatus.Queued => "queued",
        ChatExportStatus.Running => "running",
        ChatExportStatus.Ready => "ready",
        ChatExportStatus.Failed => "failed",
        ChatExportStatus.Expired => "expired",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown export status."),
    };

    public static ChatExportStatus FromWire(string value) => value switch
    {
        "queued" => ChatExportStatus.Queued,
        "running" => ChatExportStatus.Running,
        "ready" => ChatExportStatus.Ready,
        "failed" => ChatExportStatus.Failed,
        "expired" => ChatExportStatus.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown export status."),
    };
}

/// <summary>
/// The export of one person's chat data that a Workspace Owner or Admin asked for (UC-CHT-H06, CHT-021, CHT-004): the person's authored
/// messages and space memberships, optionally inside a date range. The request is recorded here and a background job builds the file.
/// The job keeps only counts and the storage key; the file holds the data and is deleted after <see cref="ChatLimits.ExportLifetimeHours"/>.
/// </summary>
/// <remarks>
/// Invariants held here: the range ends after it starts; the lifecycle is Queued, Running, then Ready or Failed, and Ready becomes
/// Expired; a Running job that a stopped process left can be queued again. The job never records message content.
/// </remarks>
public sealed class ChatExportJob : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }

    /// <summary>The Owner or Admin who asked. Only this person downloads the file.</summary>
    public Guid RequestedBy { get; private set; }

    /// <summary>The person whose data is exported.</summary>
    public Guid SubjectUserId { get; private set; }

    public DateTimeOffset? RangeFrom { get; private set; }
    public DateTimeOffset? RangeTo { get; private set; }

    public ChatExportStatus Status { get; private set; }
    public string? StorageKey { get; private set; }
    public long SizeBytes { get; private set; }

    /// <summary>The messages in the file.</summary>
    public int MessageCount { get; private set; }

    /// <summary>The spaces in the file (memberships listed).</summary>
    public int SpaceCount { get; private set; }

    /// <summary>The spaces of the person that the requester cannot see, so they are not in the file (CHT-004). A count only, never names.</summary>
    public int ExcludedSpaceCount { get; private set; }

    /// <summary>The person's messages that were already tombstones and so have no body to export.</summary>
    public int TombstoneCount { get; private set; }

    public string? FailureCode { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private ChatExportJob() : base() { }

    public static ChatExportJob Request(
        Guid accountId,
        Guid workspaceId,
        Guid requestedBy,
        Guid subjectUserId,
        DateTimeOffset? rangeFrom,
        DateTimeOffset? rangeTo,
        DateTimeOffset requestedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(requestedBy);
        Guard.NotEmpty(subjectUserId);
        if (rangeFrom is { } from && rangeTo is { } to && to <= from)
            throw new BusinessRuleException(ChatRuleCodes.ExportInvalid, "The range must end after it starts.");

        var job = new ChatExportJob
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            RequestedBy = requestedBy,
            SubjectUserId = subjectUserId,
            RangeFrom = rangeFrom,
            RangeTo = rangeTo,
            Status = ChatExportStatus.Queued,
        };
        job.SetAuditOnCreate(requestedBy, requestedAt);
        return job;
    }

    /// <summary>The worker takes the job. A job that is not queued is not taken. Returns false then.</summary>
    public bool Start(DateTimeOffset at)
    {
        if (Status != ChatExportStatus.Queued) return false;
        var pending = PrepareAuditUpdate(null, at);
        Status = ChatExportStatus.Running;
        StartedAt = at;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        return true;
    }

    /// <summary>A job a stopped process left running goes back to the queue, so the next pass builds it again.</summary>
    public bool Requeue(DateTimeOffset at)
    {
        if (Status != ChatExportStatus.Running) return false;
        var pending = PrepareAuditUpdate(null, at);
        Status = ChatExportStatus.Queued;
        StartedAt = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        return true;
    }

    public void Complete(
        string storageKey, long sizeBytes, int messageCount, int spaceCount, int excludedSpaceCount, int tombstoneCount,
        DateTimeOffset at, TimeSpan lifetime)
    {
        if (Status != ChatExportStatus.Running)
            throw new BusinessRuleException(ChatRuleCodes.ExportInvalid, "Only a running export completes.");
        Guard.NotNullOrWhiteSpace(storageKey);
        if (sizeBytes < 0 || messageCount < 0 || spaceCount < 0 || excludedSpaceCount < 0 || tombstoneCount < 0)
            throw new BusinessRuleException(ChatRuleCodes.ExportInvalid, "The counts of an export are not negative.");

        var pending = PrepareAuditUpdate(null, at);
        Status = ChatExportStatus.Ready;
        StorageKey = storageKey;
        SizeBytes = sizeBytes;
        MessageCount = messageCount;
        SpaceCount = spaceCount;
        ExcludedSpaceCount = excludedSpaceCount;
        TombstoneCount = tombstoneCount;
        CompletedAt = at;
        ExpiresAt = at + lifetime;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Fail(string failureCode, DateTimeOffset at)
    {
        if (Status is ChatExportStatus.Ready or ChatExportStatus.Expired)
            throw new BusinessRuleException(ChatRuleCodes.ExportInvalid, "A finished export does not fail.");
        Guard.NotNullOrWhiteSpace(failureCode);

        var pending = PrepareAuditUpdate(null, at);
        Status = ChatExportStatus.Failed;
        FailureCode = failureCode;
        CompletedAt = at;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    /// <summary>The file is past its lifetime: it is gone and cannot be downloaded. Returns false unless the export was ready.</summary>
    public bool Expire(DateTimeOffset at)
    {
        if (Status != ChatExportStatus.Ready) return false;
        var pending = PrepareAuditUpdate(null, at);
        Status = ChatExportStatus.Expired;
        StorageKey = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        return true;
    }

    /// <summary>True when the file can be downloaded at <paramref name="now"/>: ready, stored and not past its expiry.</summary>
    public bool IsDownloadableAt(DateTimeOffset now) =>
        Status == ChatExportStatus.Ready && StorageKey is not null && ExpiresAt is { } expires && now < expires;
}
