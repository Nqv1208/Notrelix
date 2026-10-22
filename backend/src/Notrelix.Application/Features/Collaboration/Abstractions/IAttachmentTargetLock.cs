namespace Notrelix.Application.Features.Collaboration.Abstractions;

/// <summary>
/// Serializes the final per-target attachment count check with the insert of a new attachment
/// row (file or link) inside the active request transaction. It exists because the per-item cap
/// is a count over rows that concurrent requests insert: a check made before the (possibly
/// minutes long) body stream cannot be trusted at commit time.
///
/// The lock is held only from the moment the caller acquires it, after the bytes are stored and
/// immediately before the row is added, until the request transaction commits or rolls back;
/// there is no manual release. It must never be taken before streaming a body. Implementations
/// must fail when no transaction is active rather than lock outside it.
/// </summary>
public interface IAttachmentTargetLock
{
    Task AcquireAsync(ResourceRef target, CancellationToken cancellationToken);
}
