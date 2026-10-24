using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Storage;

namespace Notrelix.Infrastructure.Tests.Storage;

public sealed class AttachmentObjectReaperTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly AttachmentRetentionOptions Options = new()
    {
        DeletedFileRetention = TimeSpan.FromDays(30),
        OrphanGracePeriod = TimeSpan.FromHours(24),
        BatchSize = 2,
    };

    private readonly FakeStorage _storage = new();
    private readonly FakeStore _store = new();

    private AttachmentObjectReaper CreateReaper(IAttachmentReaperStore? store = null) => new(
        store ?? _store, _storage, new FixedClock(), Microsoft.Extensions.Options.Options.Create(Options),
        NullLogger<AttachmentObjectReaper>.Instance);

    // ── purge after retention ──

    [Fact]
    public async Task Purge_DeletesObjectsOfRowsDeletedLongerThanRetention_AndMarksThemPurged()
    {
        var old = _store.AddDeleted(Now - TimeSpan.FromDays(31));
        var recent = _store.AddDeleted(Now - TimeSpan.FromDays(29));
        _storage.Objects[old.StorageKey] = Now - TimeSpan.FromDays(60);
        _storage.Objects[recent.StorageKey] = Now - TimeSpan.FromDays(60);

        var result = await CreateReaper().RunOnceAsync(default);

        result.Purged.Should().Be(1);
        _storage.Deleted.Should().Equal(old.StorageKey);
        _storage.Objects.Should().NotContainKey(old.StorageKey);
        old.PurgedAt.Should().Be(Now);
        recent.PurgedAt.Should().BeNull();
        _storage.Objects.Should().ContainKey(recent.StorageKey); // still inside the retention
    }

    [Fact]
    public async Task Purge_RetentionBoundary_IsExclusive_ExactlyAtTheCutoffIsKept()
    {
        var atBoundary = _store.AddDeleted(Now - Options.DeletedFileRetention);
        var justOver = _store.AddDeleted(Now - Options.DeletedFileRetention - TimeSpan.FromTicks(1));

        await CreateReaper().RunOnceAsync(default);

        atBoundary.PurgedAt.Should().BeNull();
        justOver.PurgedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Purge_IsIdempotent_ASecondRunDeletesNothingMore()
    {
        _store.AddDeleted(Now - TimeSpan.FromDays(40));
        _store.AddDeleted(Now - TimeSpan.FromDays(40));
        _store.AddDeleted(Now - TimeSpan.FromDays(40));
        var reaper = CreateReaper();

        var first = await reaper.RunOnceAsync(default);
        var second = await reaper.RunOnceAsync(default);

        first.Purged.Should().Be(3); // more than one batch (size 2) is drained in one pass
        second.Purged.Should().Be(0);
        _storage.Deleted.Should().HaveCount(3);
    }

    [Fact]
    public async Task Purge_CrashBetweenObjectDeleteAndMark_JustRepeatsTheDelete()
    {
        var row = _store.AddDeleted(Now - TimeSpan.FromDays(40));
        _store.FailCommitOnce = true;

        var crashed = () => CreateReaper().RunOnceAsync(default);
        await crashed.Should().ThrowAsync<InvalidOperationException>();
        row.PurgedAt.Should().BeNull(); // object deleted, mark lost

        await CreateReaper().RunOnceAsync(default);

        row.PurgedAt.Should().Be(Now);
        _storage.DeleteCalls.Should().Be(2); // the second delete of a missing object is a no-op success
    }

    [Fact]
    public async Task Purge_FailureOfOneObject_DoesNotStopTheBatch_AndTheRowIsRetriedLater()
    {
        var failing = _store.AddDeleted(Now - TimeSpan.FromDays(40));
        var healthy = _store.AddDeleted(Now - TimeSpan.FromDays(39));
        _storage.FailingKeys.Add(failing.StorageKey);

        var first = await CreateReaper().RunOnceAsync(default);

        first.Purged.Should().Be(1);
        healthy.PurgedAt.Should().Be(Now);
        failing.PurgedAt.Should().BeNull();

        _storage.FailingKeys.Clear();
        await CreateReaper().RunOnceAsync(default);
        failing.PurgedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Purge_NeverDeletesAKeyOutsideTheRowsTenantPrefix()
    {
        var row = _store.AddDeleted(Now - TimeSpan.FromDays(40));
        var foreign = $"{Guid.NewGuid():N}/{Guid.NewGuid():N}/attachments/{Guid.NewGuid():N}";
        _storage.Objects[foreign] = Now - TimeSpan.FromDays(60);
        row.StorageKey = foreign;

        await CreateReaper().RunOnceAsync(default);

        _storage.Objects.Should().ContainKey(foreign);
        row.PurgedAt.Should().BeNull();
    }

    // ── orphan reconciliation ──

    [Fact]
    public async Task Orphan_ObjectOlderThanGrace_WithoutRow_IsRemoved_ButReferencedAndForeignObjectsAreKept()
    {
        var orphan = NewKey();
        _storage.Objects[orphan] = Now - TimeSpan.FromHours(25);
        var referenced = _store.AddLive();
        _storage.Objects[referenced.StorageKey] = Now - TimeSpan.FromDays(10);
        _storage.Objects["not/an/attachment-key"] = Now - TimeSpan.FromDays(10);

        var result = await CreateReaper().RunOnceAsync(default);

        result.OrphansDeleted.Should().Be(1);
        _storage.Objects.Should().NotContainKey(orphan);
        _storage.Objects.Should().ContainKey(referenced.StorageKey).And.ContainKey("not/an/attachment-key");
    }

    [Fact]
    public async Task Orphan_InFlightUploadYoungerThanGrace_IsNeverReaped_EvenWithoutARowYet()
    {
        var inFlight = NewKey();
        _storage.Objects[inFlight] = Now - TimeSpan.FromHours(23);
        var exactlyAtGrace = NewKey();
        _storage.Objects[exactlyAtGrace] = Now - Options.OrphanGracePeriod;
        var justOver = NewKey();
        _storage.Objects[justOver] = Now - Options.OrphanGracePeriod - TimeSpan.FromTicks(1);

        await CreateReaper().RunOnceAsync(default);

        _storage.Objects.Should().ContainKey(inFlight).And.ContainKey(exactlyAtGrace);
        _storage.Objects.Should().NotContainKey(justOver);
    }

    [Fact]
    public async Task Orphan_ObjectOfASoftDeletedRowInsideRetention_IsNotAnOrphan()
    {
        var row = _store.AddDeleted(Now - TimeSpan.FromDays(5));
        _storage.Objects[row.StorageKey] = Now - TimeSpan.FromDays(6);

        await CreateReaper().RunOnceAsync(default);

        _storage.Objects.Should().ContainKey(row.StorageKey);
    }

    [Fact]
    public async Task Orphan_Sweep_IsIdempotent_AndSpansSeveralChunks()
    {
        var orphans = Enumerable.Range(0, 5).Select(_ => NewKey()).ToArray();
        foreach (var key in orphans)
        {
            _storage.Objects[key] = Now - TimeSpan.FromDays(3);
        }

        var first = await CreateReaper().RunOnceAsync(default);
        var second = await CreateReaper().RunOnceAsync(default);

        first.OrphansDeleted.Should().Be(5); // chunk size is 2
        second.OrphansDeleted.Should().Be(0);
        _storage.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task Orphan_FailureOfOneObject_DoesNotStopTheBatch()
    {
        var bad = NewKey();
        var good = NewKey();
        _storage.Objects[bad] = Now - TimeSpan.FromDays(3);
        _storage.Objects[good] = Now - TimeSpan.FromDays(3);
        _storage.FailingKeys.Add(bad);

        var result = await CreateReaper().RunOnceAsync(default);

        result.OrphansDeleted.Should().Be(1);
        _storage.Objects.Should().ContainKey(bad).And.NotContainKey(good);
    }

    [Fact]
    public async Task Orphan_PurgedRowDoesNotKeepAReappearedObjectAlive()
    {
        var row = _store.AddDeleted(Now - TimeSpan.FromDays(40));
        row.PurgedAt = Now - TimeSpan.FromDays(1);
        _storage.Objects[row.StorageKey] = Now - TimeSpan.FromDays(3);

        await CreateReaper().RunOnceAsync(default);

        _storage.Objects.Should().NotContainKey(row.StorageKey);
    }

    // ── multi-instance claim ──

    [Fact]
    public async Task TwoInstances_NeverProcessTheSameObjectTwice()
    {
        var row = _store.AddDeleted(Now - TimeSpan.FromDays(40));
        var orphan = NewKey();
        _storage.Objects[orphan] = Now - TimeSpan.FromDays(3);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _storage.DeleteGate = async key =>
        {
            entered.TrySetResult();
            await gate.Task; // instance A holds its claims while it deletes
        };

        var a = Task.Run(() => CreateReaper().RunOnceAsync(default));
        await entered.Task;
        _storage.DeleteGate = null;
        var b = await CreateReaper().RunOnceAsync(default); // instance B runs while A holds the claim
        gate.SetResult();
        var resultA = await a;

        b.PurgeClaimed.Should().Be(0);   // SKIP LOCKED: the row is held by A
        resultA.Purged.Should().Be(1);
        _storage.Deleted.Count(k => k == row.StorageKey).Should().Be(1);
        _storage.Deleted.Count(k => k == orphan).Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task Sweep_WhenAnotherInstanceHoldsTheClaim_IsSkipped_WithoutDeleting()
    {
        var orphan = NewKey();
        _storage.Objects[orphan] = Now - TimeSpan.FromDays(3);
        _store.SweepBusy = true;

        var result = await CreateReaper().RunOnceAsync(default);

        result.SweepSkipped.Should().BeTrue();
        _storage.Objects.Should().ContainKey(orphan);
    }

    // ── options ──

    [Fact]
    public void Options_AreValidated()
    {
        var validator = new AttachmentRetentionOptionsValidator();

        validator.Validate(null, new AttachmentRetentionOptions()).Succeeded.Should().BeTrue();
        validator.Validate(null, new AttachmentRetentionOptions { DeletedFileRetention = TimeSpan.FromMinutes(5) }).Failed.Should().BeTrue();
        validator.Validate(null, new AttachmentRetentionOptions { OrphanGracePeriod = TimeSpan.FromMinutes(5) }).Failed.Should().BeTrue();
        validator.Validate(null, new AttachmentRetentionOptions { Interval = TimeSpan.Zero }).Failed.Should().BeTrue();
        validator.Validate(null, new AttachmentRetentionOptions { BatchSize = 0 }).Failed.Should().BeTrue();
    }

    private static string NewKey() => AttachmentStorageKey.Create(Guid.NewGuid(), Guid.NewGuid());

    // ── fakes ──

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Row
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Guid AccountId { get; } = Guid.NewGuid();
        public Guid WorkspaceId { get; } = Guid.NewGuid();
        public string StorageKey { get; set; } = string.Empty;
        public DateTimeOffset? DeletedAt { get; set; }
        public DateTimeOffset? PurgedAt { get; set; }
        public bool Locked { get; set; }
    }

    private sealed class FakeStorage : IStorageService
    {
        public ConcurrentDictionary<string, DateTimeOffset> Objects { get; } = new();
        public ConcurrentQueue<string> DeletedQueue { get; } = new();
        public List<string> Deleted => DeletedQueue.ToList();
        public int DeleteCalls => DeletedQueue.Count;
        public HashSet<string> FailingKeys { get; } = [];
        public Func<string, Task>? DeleteGate { get; set; }

        public Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct) => Task.CompletedTask;
        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) =>
            Task.FromResult<Stream?>(Objects.ContainsKey(key) || FailingKeys.Contains(key) ? new MemoryStream() : null);

        public async Task DeleteAsync(string key, CancellationToken ct)
        {
            if (FailingKeys.Contains(key))
            {
                throw new IOException("simulated provider failure");
            }

            var gate = DeleteGate;
            if (gate is not null)
            {
                await gate(key);
            }

            Objects.TryRemove(key, out _);
            DeletedQueue.Enqueue(key);
        }

        public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
            string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            foreach (var pair in Objects.ToArray())
            {
                if (pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    yield return new StorageObjectInfo(pair.Key, pair.Value);
                }
            }
        }
    }

    /// <summary>Models the store contract: SKIP LOCKED row claims, an exclusive sweep claim, commit-time marking.</summary>
    private sealed class FakeStore : IAttachmentReaperStore
    {
        private readonly List<Row> _rows = [];
        private readonly object _sync = new();
        private bool _sweeping;

        public bool SweepBusy { get; set; }
        public bool FailCommitOnce { get; set; }

        public Row AddDeleted(DateTimeOffset deletedAt) => Add(deletedAt);
        public Row AddLive() => Add(null);

        private Row Add(DateTimeOffset? deletedAt)
        {
            var row = new Row { DeletedAt = deletedAt };
            row.StorageKey = AttachmentStorageKey.Create(row.AccountId, row.WorkspaceId);
            lock (_sync)
            {
                _rows.Add(row);
            }

            return row;
        }

        public async Task<AttachmentPurgeResult> PurgeDeletedAsync(
            DateTimeOffset deletedBefore, int batchSize, DateTimeOffset now,
            Func<AttachmentPurgeCandidate, CancellationToken, Task<bool>> deleteObject, CancellationToken ct)
        {
            Row[] claimed;
            lock (_sync)
            {
                claimed = _rows
                    .Where(r => r.DeletedAt is not null && r.PurgedAt is null && !r.Locked && r.DeletedAt < deletedBefore)
                    .OrderBy(r => r.DeletedAt).Take(batchSize).ToArray();
                foreach (var row in claimed)
                {
                    row.Locked = true;
                }
            }

            try
            {
                var marked = new List<Row>();
                foreach (var row in claimed)
                {
                    if (await deleteObject(new AttachmentPurgeCandidate(row.Id, row.AccountId, row.WorkspaceId, row.StorageKey), ct))
                    {
                        marked.Add(row);
                    }
                }

                if (FailCommitOnce)
                {
                    FailCommitOnce = false;
                    throw new InvalidOperationException("simulated crash before commit");
                }

                foreach (var row in marked)
                {
                    row.PurgedAt = now;
                }

                return new AttachmentPurgeResult(claimed.Length, marked.Count);
            }
            finally
            {
                foreach (var row in claimed)
                {
                    row.Locked = false;
                }
            }
        }

        public async Task<int?> DeleteUnreferencedAsync(
            IReadOnlyCollection<string> keys, Func<string, CancellationToken, Task<bool>> deleteObject, CancellationToken ct)
        {
            lock (_sync)
            {
                if (SweepBusy || _sweeping)
                {
                    return null;
                }

                _sweeping = true;
            }

            try
            {
                HashSet<string> referenced;
                lock (_sync)
                {
                    referenced = _rows.Where(r => r.PurgedAt is null).Select(r => r.StorageKey).ToHashSet();
                }

                var deleted = 0;
                foreach (var key in keys)
                {
                    if (!referenced.Contains(key) && await deleteObject(key, ct))
                    {
                        deleted++;
                    }
                }

                return deleted;
            }
            finally
            {
                lock (_sync)
                {
                    _sweeping = false;
                }
            }
        }
    }
}
