using System.Collections.Concurrent;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// Shared "database" side of the in-memory attachment target lock: one exclusive lock per target,
/// like a PostgreSQL advisory lock keyed on the target. Sessions created from the same instance
/// exclude each other.
/// </summary>
public sealed class InMemoryAttachmentTargetLocks
{
    private readonly ConcurrentDictionary<(string Kind, Guid Id), SemaphoreSlim> _locks = new();

    internal SemaphoreSlim For(ResourceRef target) =>
        _locks.GetOrAdd((target.Kind.Value, target.ResourceId), static _ => new SemaphoreSlim(1, 1));
}

/// <summary>
/// One request's view of the lock, standing in for <c>pg_advisory_xact_lock</c>: acquiring waits for
/// every other session holding the same target, and the lock is held until <see cref="EndTransaction"/>
/// (what commit or rollback does in production). Acquiring twice in one session does not deadlock.
/// </summary>
public sealed class InMemoryAttachmentTargetLock : IAttachmentTargetLock
{
    private readonly InMemoryAttachmentTargetLocks _locks;
    private readonly List<SemaphoreSlim> _held = [];

    public InMemoryAttachmentTargetLock(InMemoryAttachmentTargetLocks? locks = null)
    {
        _locks = locks ?? new InMemoryAttachmentTargetLocks();
    }

    /// <summary>Targets acquired by this session, in acquisition order.</summary>
    public List<ResourceRef> Acquired { get; } = [];

    public async Task AcquireAsync(ResourceRef target, CancellationToken cancellationToken)
    {
        var semaphore = _locks.For(target);
        if (!_held.Contains(semaphore))
        {
            await semaphore.WaitAsync(cancellationToken);
            _held.Add(semaphore);
        }

        Acquired.Add(target);
    }

    /// <summary>Ends the request transaction: releases every lock this session holds.</summary>
    public void EndTransaction()
    {
        foreach (var semaphore in _held)
        {
            semaphore.Release();
        }

        _held.Clear();
    }
}
