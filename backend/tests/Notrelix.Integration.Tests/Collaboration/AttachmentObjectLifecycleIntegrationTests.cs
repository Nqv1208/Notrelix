using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Domain.Collaboration;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Collaboration;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Infrastructure.Storage;
using Notrelix.Infrastructure.Storage.Providers;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// Attachment object lifecycle on real PostgreSQL (migrations AND the RLS policy pack applied):
/// the commit-point cap under real concurrency, the additive purged_at column under RLS, and the
/// reaper (retention purge, orphan sweep, multi-instance claim) running as a non-superuser role
/// under the system/worker context.
/// </summary>
public sealed class AttachmentObjectLifecycleIntegrationTests : IAsyncLifetime
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-0000000000D1");
    private static readonly ResourceKind BoardItemKind = ResourceKind.Create("work-management.board-item");

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_attachment_lifecycle")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "notrelix-lifecycle-" + Guid.NewGuid().ToString("N"));
    private string _reaperConnectionString = string.Empty;
    private string _workerConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
            await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
        }

        // A non-superuser login that runs as the application role: FORCE ROW LEVEL SECURITY applies to it.
        // The password is generated per run so no credential literal lives in source.
        var reaperLoginSecret = Guid.NewGuid().ToString("N");
        await ExecuteAsync($"CREATE ROLE reaper_login LOGIN PASSWORD '{reaperLoginSecret}' IN ROLE notrelix_app;");
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Username = "reaper_login",
            Password = reaperLoginSecret,
            Options = "-c role=notrelix_app",
        };
        _reaperConnectionString = builder.ConnectionString;

        // The reaper is a background job: it connects as the worker role, never as the application role.
        _workerConnectionString = await Notrelix.Testing.Integration.AppRoleLogin.CreateAsync(
            _container.GetConnectionString(), "reaper_worker_login", Notrelix.Testing.Integration.AppRoleLogin.WorkerRole);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }

    // ───────────────────────── cap under real concurrency ─────────────────────────

    [Fact]
    public async Task ConcurrentUploads_OverTheCap_ExactlyCapSucceed_AndNoOrphanObjectRemains()
    {
        const int cap = 3;
        const int uploads = 8;
        var tenant = NewTenant();
        var itemId = Guid.NewGuid();
        var gate = new StreamGate(uploads);

        var outcomes = await Task.WhenAll(Enumerable.Range(0, uploads).Select(i =>
            RunUploadRequestAsync(tenant, itemId, $"f{i}.png", new GatedBody(Png, gate), cap)));

        outcomes.Count(o => o is null).Should().Be(cap, "exactly the cap may commit");
        outcomes.Where(o => o is not null).Should().OnlyContain(
            e => e is Notrelix.Domain.Common.Exceptions.BusinessRuleException
                && ((Notrelix.Domain.Common.Exceptions.BusinessRuleException)e).RuleCode == CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded);
        (await CountRowsAsync(itemId)).Should().Be(cap);
        var rowKeys = await RowKeysAsync(itemId);
        (await ListObjectsAsync()).Should().BeEquivalentTo(rowKeys, "rejected uploads delete their object: no orphan remains");
    }

    [Fact]
    public async Task ConcurrentLinksAndFiles_CannotJointlyExceedTheCap()
    {
        const int cap = 3;
        var tenant = NewTenant();
        var itemId = Guid.NewGuid();
        var gate = new StreamGate(4);

        var files = Enumerable.Range(0, 4).Select(i =>
            RunUploadRequestAsync(tenant, itemId, $"f{i}.png", new GatedBody(Png, gate), cap));
        var links = Enumerable.Range(0, 4).Select(i => RunRequestAsync(tenant, cap, async (context, request, targetLock, ct) =>
        {
            await gate.Task; // links are admitted at the same moment the files finish streaming
            var handler = new CreateBoardItemAttachmentCommandHandler(
                context, request, new FixedClock(), targetLock, Options.Create(new AttachmentUploadOptions { MaxAttachmentsPerItem = cap }));
            await handler.Handle(new CreateBoardItemAttachmentCommand(itemId, $"l{i}", $"https://x.test/{i}", null, null, null), ct);
        }));

        var outcomes = await Task.WhenAll(files.Concat(links));

        outcomes.Count(o => o is null).Should().Be(cap);
        (await CountRowsAsync(itemId)).Should().Be(cap);
        (await ListObjectsAsync()).Should().BeEquivalentTo((await RowKeysAsync(itemId)).Where(k => k is not null));
    }

    [Fact]
    public async Task TargetLock_IsNotHeldWhileTheBodyStreams_SoOtherWritersToTheItemProceed()
    {
        var tenant = NewTenant();
        var itemId = Guid.NewGuid();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var slow = RunUploadRequestAsync(tenant, itemId, "slow.png", new HoldingBody(Png, release.Task), 50);
        await Task.Delay(300); // the slow upload is mid-stream with its transaction open

        var linkOutcome = await RunRequestAsync(tenant, 50, async (context, request, targetLock, ct) =>
        {
            var handler = new CreateBoardItemAttachmentCommandHandler(
                context, request, new FixedClock(), targetLock, Options.Create(new AttachmentUploadOptions()));
            await handler.Handle(new CreateBoardItemAttachmentCommand(itemId, "l", "https://x.test/l", null, null, null), ct);
        }).WaitAsync(TimeSpan.FromSeconds(10)); // would time out if the stream held the lock

        linkOutcome.Should().BeNull();
        slow.IsCompleted.Should().BeFalse();
        release.SetResult();
        (await slow).Should().BeNull();
        (await CountRowsAsync(itemId)).Should().Be(2);
    }

    // ───────────────────────── migration and RLS ─────────────────────────

    [Fact]
    public async Task Migration_AddsNullablePurgedAtAndThePurgeIndex_AdditivelyWithRlsStillForced()
    {
        var column = await ScalarAsync("""
            SELECT data_type || ':' || is_nullable FROM information_schema.columns
            WHERE table_schema = 'collab' AND table_name = 'attachments' AND column_name = 'purged_at'
            """);
        var index = await ScalarAsync("SELECT indexdef FROM pg_indexes WHERE schemaname = 'collab' AND indexname = 'ix_attachments_purge_pending'");
        var rls = await ScalarAsync("SELECT relrowsecurity::text || ':' || relforcerowsecurity::text FROM pg_class WHERE oid = 'collab.attachments'::regclass");

        column.Should().Be("timestamp with time zone:YES");
        index.Should().Contain("deleted_at").And.Contain("purged_at IS NULL");
        rls.Should().Be("true:true");
    }

    [Fact]
    public async Task AppRole_CanReadAndWritePurgedAtOnlyOnItsOwnTenantRows()
    {
        await TruncateAttachmentsAsync();
        var a = await SeedDeletedAsync(NewTenant(), DateTimeOffset.UtcNow.AddDays(-40));
        var b = await SeedDeletedAsync(NewTenant(), DateTimeOffset.UtcNow.AddDays(-40));
        await using (var context = CreateContext())
        {
            context.AccessGrants.Add(new AccessGrant(
                accountId: a.AccountId, workspaceId: a.WorkspaceId, userId: UserId, sourceContext: "Workspace",
                membershipStatus: "Active", roleCodes: [], permissionCodes: [], isAccountAdmin: false,
                isWorkspaceAdmin: false, grantedAt: Now));
            await context.SaveChangesAsync();
        }

        var visible = await AsAppRoleAsync($"""
            SELECT id::text FROM collab.attachments
            """);
        var updatedOwn = await AsAppRoleAsync($"UPDATE collab.attachments SET purged_at = now() WHERE id = '{a.Id}' RETURNING id::text");
        var updatedOther = await AsAppRoleAsync($"UPDATE collab.attachments SET purged_at = now() WHERE id = '{b.Id}' RETURNING id::text");

        visible.Should().Equal(a.Id.ToString());
        updatedOwn.Should().Equal(a.Id.ToString());
        updatedOther.Should().BeEmpty("the other tenant's row is invisible, so it cannot be purged by the application role");
    }

    // ───────────────────────── reaper ─────────────────────────

    [Fact]
    public async Task Reaper_PurgesObjectsAfterRetention_MarksRows_AndRespectsTheBoundary_UnderRls()
    {
        await TruncateAttachmentsAsync();
        var storage = NewStorage();
        var cutoff = Now - TimeSpan.FromDays(30);
        var tenantA = NewTenant();
        var tenantB = NewTenant();
        var overdueA = await SeedDeletedAsync(tenantA, cutoff - TimeSpan.FromSeconds(1), storage);
        var overdueB = await SeedDeletedAsync(tenantB, cutoff - TimeSpan.FromDays(5), storage);
        var atBoundary = await SeedDeletedAsync(tenantA, cutoff, storage);
        var recent = await SeedDeletedAsync(tenantA, Now - TimeSpan.FromDays(2), storage);
        var live = await SeedLiveAsync(tenantA, storage);

        var result = await RunReaperAsync(storage);

        result.Purged.Should().Be(2);
        (await PurgedAtAsync(overdueA.Id)).Should().NotBeNull();
        (await PurgedAtAsync(overdueB.Id)).Should().NotBeNull();
        (await PurgedAtAsync(atBoundary.Id)).Should().BeNull();
        (await PurgedAtAsync(recent.Id)).Should().BeNull();
        (await PurgedAtAsync(live.Id)).Should().BeNull();
        (await ListObjectsAsync()).Should().BeEquivalentTo(
            [atBoundary.Metadata.StorageKey!, recent.Metadata.StorageKey!, live.Metadata.StorageKey!]);

        var rerun = await RunReaperAsync(storage);
        rerun.Purged.Should().Be(0, "a second pass is a no-op");
    }

    [Fact]
    public async Task Reaper_WithoutTheSystemContext_SeesNoRows_SoRlsIsNotBypassed()
    {
        await TruncateAttachmentsAsync();
        await SeedDeletedAsync(NewTenant(), Now - TimeSpan.FromDays(60), NewStorage());

        await using var context = CreateContext(connectionString: _reaperConnectionString);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var visible = await context.Attachments.IgnoreQueryFilters().CountAsync();

        visible.Should().Be(0, "the application login has no tenant context: only the worker login sees rows");
    }

    [Fact]
    public async Task Reaper_RemovesOrphanObjects_OlderThanGrace_ButNotInFlightOrReferencedOnes()
    {
        await TruncateAttachmentsAsync();
        var storage = NewStorage();
        var referenced = await SeedLiveAsync(NewTenant(), storage);
        File.SetLastWriteTimeUtc(PathOf(referenced.Metadata.StorageKey!), DateTime.UtcNow.AddDays(-10));
        var orphan = await WriteObjectAsync(storage, NewTenant(), DateTime.UtcNow.AddDays(-2));
        var inFlight = await WriteObjectAsync(storage, NewTenant(), DateTime.UtcNow.AddHours(-1));

        var result = await RunReaperAsync(storage, clock: new FixedClock(DateTimeOffset.UtcNow));

        result.OrphansDeleted.Should().Be(1);
        (await ListObjectsAsync()).Should().BeEquivalentTo([referenced.Metadata.StorageKey!, inFlight]);
        (await ListObjectsAsync()).Should().NotContain(orphan);
    }

    [Fact]
    public async Task Reaper_TwoInstances_PurgeEveryObjectExactlyOnce_AndOnlyOneSweeps()
    {
        await TruncateAttachmentsAsync();
        var inner = NewStorage();
        var tenant = NewTenant();
        for (var i = 0; i < 6; i++)
        {
            await SeedDeletedAsync(tenant, Now - TimeSpan.FromDays(60), inner);
        }

        var orphan = await WriteObjectAsync(inner, NewTenant(), DateTime.UtcNow.AddDays(-5));
        var storage = new SlowCountingStorage(inner, TimeSpan.FromMilliseconds(250));
        var options = new AttachmentRetentionOptions { BatchSize = 3 };

        var results = await Task.WhenAll(
            RunReaperAsync(storage, options, new FixedClock(DateTimeOffset.UtcNow.AddDays(1))),
            RunReaperAsync(storage, options, new FixedClock(DateTimeOffset.UtcNow.AddDays(1))));

        results.Sum(r => r.Purged).Should().Be(6);
        storage.DeleteCounts.Values.Should().OnlyContain(count => count == 1, "SKIP LOCKED claims and the sweep claim prevent a double delete");
        storage.DeleteCounts.Should().ContainKey(orphan);
        (await ListObjectsAsync()).Should().BeEmpty();
    }

    // ───────────────────────── harness ─────────────────────────

    private sealed record Tenant(Guid AccountId, Guid WorkspaceId);

    private static Tenant NewTenant() => new(Guid.NewGuid(), Guid.NewGuid());

    private sealed class FixedClock(DateTimeOffset? at = null) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => at ?? Now;
    }

    private LocalStorageProvider NewStorage() =>
        new(Options.Create(new StorageOptions { BasePath = _storageRoot }));

    private string PathOf(string key) => Path.Combine(_storageRoot, Path.Combine(key.Split('/')));

    private async Task<List<string>> ListObjectsAsync()
    {
        var list = new List<string>();
        await foreach (var info in NewStorage().ListAsync(string.Empty, default))
        {
            list.Add(info.Key);
        }

        return list;
    }

    private async Task<string> WriteObjectAsync(IStorageService storage, Tenant tenant, DateTime lastWriteUtc)
    {
        var key = AttachmentStorageKey.Create(tenant.AccountId, tenant.WorkspaceId);
        await storage.WriteAsync(key, new MemoryStream(Png), "image/png", default);
        File.SetLastWriteTimeUtc(PathOf(key), lastWriteUtc);
        return key;
    }

    private ApplicationDbContext CreateContext(ICurrentTenantContext? tenant = null, string? connectionString = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString ?? _container.GetConnectionString(), npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>()
            .Options;
        return tenant is not null ? new ApplicationDbContext(options, tenant) : new ApplicationDbContext(options);
    }

    private static CurrentTenantContext SystemTenant()
    {
        var tenant = new CurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private Task<Attachment> SeedLiveAsync(Tenant tenant, IStorageService storage) => SeedAsync(tenant, null, storage);

    private Task<Attachment> SeedDeletedAsync(Tenant tenant, DateTimeOffset deletedAt, IStorageService? storage = null) =>
        SeedAsync(tenant, deletedAt, storage);

    /// <summary>Inserts one stored-file row (soft-deleted when <paramref name="deletedAt"/> is given); writes its object when a storage is given.</summary>
    private async Task<Attachment> SeedAsync(Tenant tenant, DateTimeOffset? deletedAt, IStorageService? storage)
    {
        var key = storage is null
            ? AttachmentStorageKey.Create(tenant.AccountId, tenant.WorkspaceId)
            : await WriteObjectAsync(storage, tenant, DateTime.UtcNow);
        var attachment = Attachment.CreateStoredFile(
            tenant.AccountId, tenant.WorkspaceId,
            ResourceRef.Create(BoardItemKind, Guid.NewGuid(), tenant.WorkspaceId),
            AttachmentType.Image, "a.png", Png.Length, "image/png", key, 1024, 0, 50, UserId, DateTimeOffset.UtcNow.AddDays(-200));
        if (deletedAt is not null)
        {
            attachment.Delete(UserId, deletedAt.Value);
        }

        await using var context = CreateContext(SystemTenant());
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync();
        return attachment;
    }

    private Task<AttachmentReaperResult> RunReaperAsync(
        IStorageService storage, AttachmentRetentionOptions? options = null, IDateTimeProvider? clock = null)
    {
        var tenant = new CurrentTenantContext();
        var context = CreateContext(tenant, _workerConnectionString);
        var rls = new RlsSessionContext(context, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant);
        var store = new PostgresAttachmentReaperStore(context, tenant, rls, NullLogger<PostgresAttachmentReaperStore>.Instance);
        var reaper = new AttachmentObjectReaper(
            store, storage, clock ?? new FixedClock(), Options.Create(options ?? new AttachmentRetentionOptions()),
            NullLogger<AttachmentObjectReaper>.Instance);
        return RunAndDisposeAsync(reaper, context);
    }

    private static async Task<AttachmentReaperResult> RunAndDisposeAsync(AttachmentObjectReaper reaper, ApplicationDbContext context)
    {
        await using (context)
        {
            return await reaper.RunOnceAsync(default);
        }
    }

    /// <summary>One upload request through the real data session (transaction, commit, rollback compensations).</summary>
    private Task<Exception?> RunUploadRequestAsync(Tenant tenant, Guid itemId, string fileName, Stream body, int cap) =>
        RunRequestAsync(tenant, cap, async (context, request, targetLock, ct) =>
        {
            var compensations = (IRequestRollbackCompensations)request.Items["comp"];
            var handler = new UploadBoardItemAttachmentCommandHandler(
                context, request, new FixedClock(), NewStorage(), compensations, targetLock,
                Options.Create(new AttachmentUploadOptions { MaxAttachmentsPerItem = cap }));
            await handler.Handle(new UploadBoardItemAttachmentCommand(itemId, fileName, body), ct);
        });

    private async Task<Exception?> RunRequestAsync(
        Tenant tenant, int cap,
        Func<ApplicationDbContext, RequestScope, PostgresAttachmentTargetLock, CancellationToken, Task> action)
    {
        var request = new RequestScope { UserId = UserId };
        request.Tenant.SetWorkspace(tenant.AccountId, tenant.WorkspaceId, UserId);
        var compensations = new RequestRollbackCompensations(NullLogger<RequestRollbackCompensations>.Instance);
        request.Items["comp"] = compensations;

        await using var context = CreateContext(request.Tenant);
        var rls = new RlsSessionContext(context, Options.Create(new RlsOptions()), request.Tenant);
        var session = new EfRequestDataSession(
            context, rls, NullLogger<EfRequestDataSession>.Instance, compensations: compensations);
        var targetLock = new PostgresAttachmentTargetLock(context);
        try
        {
            await session.ExecuteAsync(
                new RequestDataSessionOptions(RequestDataAccess.Transactional, false, false),
                async ct =>
                {
                    await action(context, request, targetLock, ct);
                    return 0;
                },
                default);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private sealed class RequestScope : ICurrentRequestContext
    {
        private readonly FakeCurrentRequestContext _inner = new();
        public RequestScope() => _inner.IsAuthenticated = true;
        public Dictionary<string, object> Items { get; } = [];
        public FakeCurrentTenantContext Tenant => _inner.Tenant;
        public Guid UserId { get => _inner.UserId; set => _inner.UserId = value; }
        public string Email => _inner.Email;
        public string Name => _inner.Name;
        public bool IsAuthenticated => _inner.IsAuthenticated;
        public bool IsSystemContext => _inner.IsSystemContext;
        public Guid? SessionId => _inner.SessionId;
        public Guid RequireAccountId() => _inner.RequireAccountId();
        public Guid RequireWorkspaceId() => _inner.RequireWorkspaceId();
    }

    private sealed class StreamGate(int expected)
    {
        private readonly TaskCompletionSource _open = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;
        public Task Task => _open.Task;

        public Task ArriveAsync()
        {
            if (Interlocked.Increment(ref _arrived) >= expected)
            {
                _open.TrySetResult();
            }

            return _open.Task;
        }
    }

    /// <summary>A body whose first read waits until every concurrent upload has been admitted (passed its early cap check).</summary>
    private sealed class GatedBody(byte[] bytes, StreamGate gate) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);
        private bool _arrived;

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_arrived)
            {
                _arrived = true;
                await gate.ArriveAsync().WaitAsync(cancellationToken);
            }

            return await _inner.ReadAsync(buffer, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class HoldingBody(byte[] bytes, Task release) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);
        private bool _held;

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_held)
            {
                _held = true;
                await release.WaitAsync(cancellationToken);
            }

            return await _inner.ReadAsync(buffer, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class SlowCountingStorage(IStorageService inner, TimeSpan deleteDelay) : IStorageService
    {
        public ConcurrentDictionary<string, int> DeleteCounts { get; } = new();

        public Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct) =>
            inner.WriteAsync(key, content, contentType, ct);

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) => inner.OpenReadAsync(key, ct);

        public async Task DeleteAsync(string key, CancellationToken ct)
        {
            DeleteCounts.AddOrUpdate(key, 1, (_, n) => n + 1);
            await Task.Delay(deleteDelay, ct);
            await inner.DeleteAsync(key, ct);
        }

        public IAsyncEnumerable<StorageObjectInfo> ListAsync(string prefix, CancellationToken ct) => inner.ListAsync(prefix, ct);
    }

    // ── SQL helpers ──

    private Task<long> CountRowsAsync(Guid itemId) => ScalarAsync($"SELECT count(*)::text FROM collab.attachments WHERE resource_id = '{itemId}' AND deleted_at IS NULL")
        .ContinueWith(t => long.Parse(t.Result), TaskScheduler.Default);

    private async Task<List<string?>> RowKeysAsync(Guid itemId)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT storage_key FROM collab.attachments WHERE resource_id = @id AND deleted_at IS NULL", connection);
        command.Parameters.AddWithValue("id", itemId);
        var keys = new List<string?>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            keys.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
        }

        return keys;
    }

    private async Task<DateTimeOffset?> PurgedAtAsync(Guid id)
    {
        var value = await ScalarAsync($"SELECT COALESCE(purged_at::text, '') FROM collab.attachments WHERE id = '{id}'");
        return string.IsNullOrEmpty(value) ? null : DateTimeOffset.Parse(value);
    }

    private Task TruncateAttachmentsAsync() => ExecuteAsync("TRUNCATE collab.attachments");

    private async Task<string> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (await command.ExecuteScalarAsync())?.ToString() ?? string.Empty;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<string>> AsAppRoleAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var setup = new NpgsqlCommand($"""
            SET LOCAL ROLE notrelix_app;
            SELECT set_config('app.current_user_id', '{UserId}', true);
            SELECT set_config('app.request_scope', 'app', true);
            """, connection, transaction))
        {
            await setup.ExecuteNonQueryAsync();
        }

        var rows = new List<string>();
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                rows.Add(reader.GetString(0));
            }
        }

        await transaction.CommitAsync();
        return rows;
    }
}
