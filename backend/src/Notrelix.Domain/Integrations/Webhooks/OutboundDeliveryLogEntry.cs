namespace Notrelix.Domain.Integrations.Webhooks;

/// <summary>Where one outbound webhook delivery stands, as the member reads it in the delivery log (UC-INT-F05).</summary>
public enum OutboundDeliveryStatus
{
    /// <summary>Recorded, no attempt has reached the receiver yet.</summary>
    Pending,

    /// <summary>An attempt failed in a way another attempt may fix; another attempt is queued.</summary>
    Retrying,

    /// <summary>The receiver answered 2xx.</summary>
    Sent,

    /// <summary>The attempt budget is used up, or another attempt cannot help. Visible to the member.</summary>
    Failed,

    /// <summary>The delivery was never sent because its connection was paused, revoked or deleted (UC-INT-L02).</summary>
    Skipped,
}

/// <summary>Why the last attempt did not reach the receiver. Never the receiver's body, a header or a secret.</summary>
public enum OutboundDeliveryFailureClass
{
    /// <summary>The receiver answered with a 4xx that another attempt cannot fix.</summary>
    ReceiverRejected,

    /// <summary>The receiver answered with a 5xx.</summary>
    ReceiverError,

    /// <summary>The receiver answered 429 or 425 (provider rate limit).</summary>
    RateLimited,

    /// <summary>The receiver did not answer in time.</summary>
    Timeout,

    /// <summary>The receiver could not be reached.</summary>
    Unreachable,

    /// <summary>The address points to a network the webhook may not reach.</summary>
    AddressRefused,

    /// <summary>The connection has no signing secret to sign the delivery with.</summary>
    SecretMissing,
}

/// <summary>Why a queued delivery was skipped instead of sent (UC-INT-L02).</summary>
public enum OutboundDeliverySkipReason
{
    ConnectionPaused,
    ConnectionRevoked,
    ConnectionDeleted,
    ConnectionMissing,
}

/// <summary>
/// One row of the delivery log for an outgoing webhook connection. It keeps the delivery identity, the event
/// type, the attempt count, the last response status and the failure class. It never keeps the payload, the
/// receiver's body, a header value or a secret, and it survives a disconnect so the history stays readable.
/// </summary>
public class OutboundDeliveryLogEntry : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid ConnectionId { get; private set; }
    public string EventType { get; private set; } = null!;
    public OutboundDeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public OutboundDeliveryFailureClass? FailureClass { get; private set; }
    public OutboundDeliverySkipReason? SkipReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }

    private OutboundDeliveryLogEntry() : base() { }

    /// <summary>The delivery identity is the id, so a redelivered message finds the same row.</summary>
    public static OutboundDeliveryLogEntry Create(
        Guid deliveryId, Guid accountId, Guid workspaceId, Guid connectionId, string eventType, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(deliveryId);
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(connectionId);
        Guard.NotNullOrWhiteSpace(eventType);

        return new OutboundDeliveryLogEntry(deliveryId)
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ConnectionId = connectionId,
            EventType = eventType.Trim(),
            Status = OutboundDeliveryStatus.Pending,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    private OutboundDeliveryLogEntry(Guid id) : base(id) { }

    public bool IsTerminal => Status is OutboundDeliveryStatus.Sent or OutboundDeliveryStatus.Failed or OutboundDeliveryStatus.Skipped;

    /// <summary>Counts the attempt that is starting. A terminal delivery does not change.</summary>
    public void StartAttempt(int attemptNumber, DateTimeOffset at)
    {
        if (IsTerminal) return;
        AttemptCount = Math.Max(AttemptCount, attemptNumber);
        UpdatedAt = at;
    }

    public void RecordDelivered(int statusCode, DateTimeOffset at)
    {
        if (IsTerminal) return;
        Status = OutboundDeliveryStatus.Sent;
        ResponseStatusCode = statusCode;
        FailureClass = null;
        DeliveredAt = at;
        UpdatedAt = at;
    }

    public void RecordRetrying(int? statusCode, OutboundDeliveryFailureClass failureClass, DateTimeOffset at)
    {
        if (IsTerminal) return;
        Status = OutboundDeliveryStatus.Retrying;
        ResponseStatusCode = statusCode;
        FailureClass = failureClass;
        UpdatedAt = at;
    }

    public void RecordFailed(int? statusCode, OutboundDeliveryFailureClass failureClass, DateTimeOffset at)
    {
        if (IsTerminal) return;
        Status = OutboundDeliveryStatus.Failed;
        ResponseStatusCode = statusCode;
        FailureClass = failureClass;
        UpdatedAt = at;
    }

    public void RecordSkipped(OutboundDeliverySkipReason reason, DateTimeOffset at)
    {
        if (IsTerminal) return;
        Status = OutboundDeliveryStatus.Skipped;
        SkipReason = reason;
        UpdatedAt = at;
    }
}
