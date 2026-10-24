using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Identity.Profiles.Services;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Storage;

namespace Notrelix.Infrastructure.Tests.Storage;

/// <summary>
/// The orchestration of the profile photo sweep: which objects are offered to the store (avatar keys
/// older than the grace period, in batches), what happens when another instance holds the claim, and
/// that a failing delete is retried by the next pass. Whether an object is referenced is decided by the
/// store against PostgreSQL and is proven, under a non-superuser role, in the integration tests.
/// </summary>
public sealed class AvatarObjectReaperTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly AttachmentRetentionOptions Options = new() { OrphanGracePeriod = TimeSpan.FromHours(24), BatchSize = 2 };

    private readonly FakeStorage _storage = new();
    private readonly FakeStore _store = new();

    private AvatarObjectReaper Reaper() => new(
        _store, _storage, new FixedClock(), Microsoft.Extensions.Options.Options.Create(Options),
        NullLogger<AvatarObjectReaper>.Instance);

    private static string NewKey() => AvatarStorageKey.Create(Guid.NewGuid(), new AvatarFormat("image/png", "png"));

    [Fact]
    public async Task OffersOnlyAvatarObjectsOlderThanTheGracePeriod_AndLeavesEverythingElseAlone()
    {
        var old = NewKey();
        var young = NewKey();
        var foreign = $"{Guid.NewGuid():N}/{Guid.NewGuid():N}/attachments/{Guid.NewGuid():N}";
        _storage.Objects[old] = Now - TimeSpan.FromHours(25);
        _storage.Objects[young] = Now - TimeSpan.FromHours(23);
        _storage.Objects["users/not-an-avatar.png"] = Now - TimeSpan.FromDays(9);
        _storage.Objects[foreign] = Now - TimeSpan.FromDays(9);

        var result = await Reaper().RunOnceAsync(default);

        _store.Offered.Should().Equal(old);
        result.Should().Be((1, false));
        _storage.Objects.Keys.Should().BeEquivalentTo([young, "users/not-an-avatar.png", foreign]);
    }

    [Fact]
    public async Task TheGraceBoundaryIsExclusive_ExactlyAtTheCutoffIsKept()
    {
        var atBoundary = NewKey();
        _storage.Objects[atBoundary] = Now - Options.OrphanGracePeriod;

        await Reaper().RunOnceAsync(default);

        _store.Offered.Should().BeEmpty();
    }

    [Fact]
    public async Task SweepsInBatches_AndASecondPassFindsNothingMore()
    {
        for (var i = 0; i < 5; i++)
        {
            _storage.Objects[NewKey()] = Now - TimeSpan.FromDays(2);
        }

        var first = await Reaper().RunOnceAsync(default);
        var second = await Reaper().RunOnceAsync(default);

        first.Deleted.Should().Be(5);
        _store.Batches.Should().Equal(2, 2, 1);
        second.Deleted.Should().Be(0);
    }

    [Fact]
    public async Task WhenAnotherInstanceHoldsTheClaim_NothingIsDeleted_AndThePassReportsSkipped()
    {
        _storage.Objects[NewKey()] = Now - TimeSpan.FromDays(2);
        _store.Busy = true;

        var result = await Reaper().RunOnceAsync(default);

        result.Should().Be((0, true));
        _storage.Deleted.Should().BeEmpty();
    }

    [Fact]
    public async Task AFailingDelete_IsLogged_DoesNotStopTheBatch_AndIsRetriedByTheNextPass()
    {
        var failing = NewKey();
        var healthy = NewKey();
        _storage.Objects[failing] = Now - TimeSpan.FromDays(2);
        _storage.Objects[healthy] = Now - TimeSpan.FromDays(2);
        _storage.FailingKeys.Add(failing);

        var first = await Reaper().RunOnceAsync(default);

        first.Deleted.Should().Be(1);
        _storage.Objects.Keys.Should().BeEquivalentTo([failing]);

        _storage.FailingKeys.Clear();
        (await Reaper().RunOnceAsync(default)).Deleted.Should().Be(1);
        _storage.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task ObjectsTheStoreCannotProveUnreferenced_AreNeverDeleted()
    {
        var kept = NewKey();
        _storage.Objects[kept] = Now - TimeSpan.FromDays(2);
        _store.Referenced.Add(kept);

        var result = await Reaper().RunOnceAsync(default);

        result.Deleted.Should().Be(0);
        _storage.Objects.Keys.Should().BeEquivalentTo([kept]);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeStorage : IStorageService
    {
        public ConcurrentDictionary<string, DateTimeOffset> Objects { get; } = new();
        public List<string> Deleted { get; } = [];
        public HashSet<string> FailingKeys { get; } = [];

        public Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct) => Task.CompletedTask;
        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) => Task.FromResult<Stream?>(null);

        public Task DeleteAsync(string key, CancellationToken ct)
        {
            if (FailingKeys.Contains(key))
            {
                throw new IOException("simulated provider failure");
            }

            Objects.TryRemove(key, out _);
            Deleted.Add(key);
            return Task.CompletedTask;
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

    private sealed class FakeStore : IAvatarReaperStore
    {
        public bool Busy { get; set; }
        public HashSet<string> Referenced { get; } = [];
        public List<string> Offered { get; } = [];
        public List<int> Batches { get; } = [];

        public async Task<int?> DeleteUnreferencedAsync(
            IReadOnlyCollection<string> keys,
            Func<string, CancellationToken, Task<bool>> deleteObject,
            CancellationToken cancellationToken)
        {
            if (Busy)
            {
                return null;
            }

            Batches.Add(keys.Count);
            Offered.AddRange(keys);
            var deleted = 0;
            foreach (var key in keys)
            {
                if (!Referenced.Contains(key) && await deleteObject(key, cancellationToken))
                {
                    deleted++;
                }
            }

            return deleted;
        }
    }
}
