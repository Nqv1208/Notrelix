namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Persistence side of the avatar object reaper: one short transaction under the explicit worker
/// (system) context that owns its claim and commits after the per-object callbacks ran.
/// </summary>
internal interface IAvatarReaperStore
{
    /// <summary>
    /// Under an exclusive sweep claim, removes through <paramref name="deleteObject"/> every key of
    /// <paramref name="keys"/> that no user row references. Returns the number of objects deleted, or
    /// null when another instance holds the sweep claim and nothing was done.
    /// </summary>
    Task<int?> DeleteUnreferencedAsync(
        IReadOnlyCollection<string> keys,
        Func<string, CancellationToken, Task<bool>> deleteObject,
        CancellationToken cancellationToken);
}
