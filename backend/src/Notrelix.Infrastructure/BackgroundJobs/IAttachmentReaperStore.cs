namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>A soft-deleted stored-file attachment whose object is due for purge.</summary>
internal sealed record AttachmentPurgeCandidate(Guid AttachmentId, Guid AccountId, Guid WorkspaceId, string StorageKey);

internal sealed record AttachmentPurgeResult(int Claimed, int Purged);

/// <summary>
/// Persistence side of the attachment object reaper. Every method is one short transaction that
/// runs under the explicit worker (system) context, owns its claim, and commits only after the
/// per-object callbacks ran. Object deletion happens in the callbacks, before the row or claim is
/// finalised: a crash between "object deleted" and "row marked" only repeats an idempotent delete.
/// </summary>
internal interface IAttachmentReaperStore
{
    /// <summary>
    /// Claims up to <paramref name="batchSize"/> soft-deleted stored-file rows that were deleted
    /// strictly before <paramref name="deletedBefore"/> and are not purged yet, skipping rows another
    /// instance already holds. For each claimed row <paramref name="deleteObject"/> runs; when it
    /// returns true the row is marked purged at <paramref name="now"/>.
    /// </summary>
    Task<AttachmentPurgeResult> PurgeDeletedAsync(
        DateTimeOffset deletedBefore,
        int batchSize,
        DateTimeOffset now,
        Func<AttachmentPurgeCandidate, CancellationToken, Task<bool>> deleteObject,
        CancellationToken cancellationToken);

    /// <summary>
    /// Under an exclusive sweep claim, removes through <paramref name="deleteObject"/> every key of
    /// <paramref name="keys"/> that no non-purged attachment row references. Returns the number of
    /// objects deleted, or null when another instance holds the sweep claim and nothing was done.
    /// </summary>
    Task<int?> DeleteUnreferencedAsync(
        IReadOnlyCollection<string> keys,
        Func<string, CancellationToken, Task<bool>> deleteObject,
        CancellationToken cancellationToken);
}
