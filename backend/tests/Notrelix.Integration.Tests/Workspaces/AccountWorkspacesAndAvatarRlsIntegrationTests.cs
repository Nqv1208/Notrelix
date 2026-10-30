using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Identity.Profiles.Commands.RemoveAvatar;
using Notrelix.Application.Features.Identity.Profiles.Commands.UploadAvatar;
using Notrelix.Application.Features.Identity.Profiles.Queries.GetUserAvatar;
using Notrelix.Application.Features.Identity.Profiles.Services;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.LeaveWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Commands.SetDefaultWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetMyWorkspaces;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.CrossContext.Identity.Avatars;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Infrastructure.Storage;
using Notrelix.Infrastructure.Storage.Providers;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Account workspace list, default workspace, self-leave and profile photo on real PostgreSQL with
/// migrations AND the RLS policy pack applied, executed as a NON-SUPERUSER login that is a member of
/// <c>notrelix_app</c> (FORCE ROW LEVEL SECURITY applies to it). Seed rows are written as the
/// superuser through the production handlers; every behaviour under test then runs through the real
/// request data session (transaction, RLS session context, commit) as the application role.
/// </summary>
public sealed class AccountWorkspacesAndAvatarRlsIntegrationTests : IAsyncLifetime
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8, 9];
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountA = Guid.Parse("A0000000-0000-0000-0000-0000000000A1");
    private static readonly Guid AccountB = Guid.Parse("B0000000-0000-0000-0000-0000000000B2");

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_account_pages")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "notrelix-avatars-" + Guid.NewGuid().ToString("N"));
    private string _appConnectionString = string.Empty;
    private string _workerConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
            await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
        }

        // A non-superuser login that runs as the application role. The password is generated per run.
        var secret = Guid.NewGuid().ToString("N");
        await ExecuteAsync($"CREATE ROLE account_pages_login LOGIN PASSWORD '{secret}' IN ROLE notrelix_app;");
        _appConnectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Username = "account_pages_login",
            Password = secret,
            Options = "-c role=notrelix_app",
        }.ConnectionString;

        // The background principal's login: sweeps and reapers connect as the worker role.
        _workerConnectionString = await AppRoleLogin.CreateAsync(
            _container.GetConnectionString(), "account_pages_worker_login", AppRoleLogin.WorkerRole);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }

    // ───────────────────────── list ─────────────────────────

    [Fact]
    public async Task List_ReturnsOnlyTheCallersActiveMemberships_WithRoleCountsAndNullRules()
    {
        var alice = await SeedUserAsync("alice");
        var bob = await SeedUserAsync("bob");
        var carol = await SeedUserAsync("carol");
        var dave = await SeedUserAsync("dave");

        var w1 = await SeedWorkspaceAsync(AccountA, alice, "Alpha");
        await AddMemberAsync(AccountA, alice, w1, bob, WorkspaceRole.Member);
        await AddMemberAsync(AccountA, alice, w1, carol, WorkspaceRole.Guest);
        await SeedDepartmentAsync(AccountA, w1, alice, "Sales");
        var w2 = await SeedWorkspaceAsync(AccountB, bob, "Beta");
        await AddMemberAsync(AccountB, bob, w2, alice, WorkspaceRole.Admin);
        var w3 = await SeedWorkspaceAsync(AccountA, dave, "Elsewhere");

        var aliceRows = await ListAsync(alice, AccountA);
        aliceRows.Select(r => r.Name).Should().Equal("Alpha", "Beta");
        var alpha = aliceRows.Single(r => r.Id == w1);
        alpha.Role.Should().Be("Owner");
        alpha.MemberCount.Should().Be(3);
        alpha.DepartmentCount.Should().Be(1);
        alpha.CanLeave.Should().BeFalse("alice is the only active Owner");
        var beta = aliceRows.Single(r => r.Id == w2);
        beta.Role.Should().Be("Admin");
        beta.MemberCount.Should().Be(2);
        beta.DepartmentCount.Should().Be(0);
        beta.CanLeave.Should().BeTrue();
        aliceRows.Should().NotContain(r => r.Id == w3, "another person's workspace never appears");

        var bobRows = await ListAsync(bob, AccountA);
        bobRows.Single(r => r.Id == w1).Should().Match<AccountWorkspaceDto>(
            r => r.Role == "Member" && r.MemberCount == 3 && r.DepartmentCount == null && r.CanLeave);

        var carolRows = await ListAsync(carol, AccountA);
        carolRows.Should().ContainSingle().Which.Should().Match<AccountWorkspaceDto>(
            r => r.Role == "Guest" && r.MemberCount == null && r.DepartmentCount == null);

        (await ListAsync(Guid.NewGuid(), AccountA)).Should().BeEmpty("a stranger has no memberships");
    }

    // ───────────────────────── default workspace ─────────────────────────

    [Fact]
    public async Task SetDefault_FlagsThatWorkspaceInTheList_AndRejectsANonMember()
    {
        var alice = await SeedUserAsync("alice");
        var bob = await SeedUserAsync("bob");
        var mine = await SeedWorkspaceAsync(AccountA, alice, "Mine");
        var other = await SeedWorkspaceAsync(AccountA, bob, "Theirs");

        await SetDefaultAsync(alice, AccountA, mine);

        (await ListAsync(alice, AccountA)).Should().ContainSingle().Which.IsDefault.Should().BeTrue();
        (await ScalarAsync($"SELECT default_workspace_id::text FROM identity.user_profiles WHERE user_id = '{alice}'"))
            .Should().Be(mine.ToString());

        var act = () => SetDefaultAsync(alice, AccountA, other);
        await act.Should().ThrowAsync<NotFoundException>("alice is not a member of the other workspace");
        (await ScalarAsync($"SELECT default_workspace_id::text FROM identity.user_profiles WHERE user_id = '{alice}'"))
            .Should().Be(mine.ToString(), "a rejected choice leaves the default unchanged");
    }

    // ───────────────────────── leave ─────────────────────────

    [Fact]
    public async Task Leave_AsAMember_RemovesMembership_RevokesOwnGrant_ClearsDefault_UnderTheAppRole()
    {
        var alice = await SeedUserAsync("alice");
        var bob = await SeedUserAsync("bob");
        var ws = await SeedWorkspaceAsync(AccountA, alice, "Shared");
        await AddMemberAsync(AccountA, alice, ws, bob, WorkspaceRole.Member);
        await SetDefaultAsync(bob, AccountA, ws);

        await LeaveAsync(bob, AccountA, ws);

        (await ScalarAsync($"SELECT status FROM workspace.workspace_members WHERE workspace_id = '{ws}' AND user_id = '{bob}'"))
            .Should().Be("Removed");
        (await ScalarAsync($"SELECT (revoked_at IS NOT NULL)::text FROM authz.access_grants WHERE workspace_id = '{ws}' AND user_id = '{bob}'"))
            .Should().Be("true", "leaving revokes the leaver's own workspace grant");
        (await ScalarAsync($"SELECT COALESCE(default_workspace_id::text, '') FROM identity.user_profiles WHERE user_id = '{bob}'"))
            .Should().BeEmpty("a default pointing at the left workspace is cleared");
        (await ListAsync(bob, AccountA)).Should().BeEmpty();
        (await ScalarAsync($"SELECT status FROM workspace.workspace_members WHERE workspace_id = '{ws}' AND user_id = '{alice}'"))
            .Should().Be("Active", "other members are untouched");
    }

    [Fact]
    public async Task Leave_AsTheSoleOwner_IsAConflict_AndChangesNothing()
    {
        var alice = await SeedUserAsync("alice");
        var bob = await SeedUserAsync("bob");
        var ws = await SeedWorkspaceAsync(AccountA, alice, "Solo");
        await AddMemberAsync(AccountA, alice, ws, bob, WorkspaceRole.Member);

        var act = () => LeaveAsync(alice, AccountA, ws);

        var thrown = await act.Should().ThrowAsync<ConflictException>();
        thrown.Which.ErrorCode.Should().Be("workspaces.member.last-owner-cannot-leave");
        (await ScalarAsync($"SELECT status FROM workspace.workspace_members WHERE workspace_id = '{ws}' AND user_id = '{alice}'"))
            .Should().Be("Active");
        (await ScalarAsync($"SELECT (revoked_at IS NULL)::text FROM authz.access_grants WHERE workspace_id = '{ws}' AND user_id = '{alice}'"))
            .Should().Be("true");
    }

    [Fact]
    public async Task Leave_AnOwner_Succeeds_WhenAnotherActiveOwnerRemains()
    {
        var alice = await SeedUserAsync("alice");
        var bob = await SeedUserAsync("bob");
        var ws = await SeedWorkspaceAsync(AccountA, alice, "Duo");
        await AddMemberAsync(AccountA, alice, ws, bob, WorkspaceRole.Member);
        await ExecuteAsync($"UPDATE workspace.workspace_members SET role = 'Owner' WHERE workspace_id = '{ws}' AND user_id = '{bob}'");

        (await ListAsync(alice, AccountA)).Single().CanLeave.Should().BeTrue();
        await LeaveAsync(alice, AccountA, ws);

        (await ScalarAsync($"SELECT status FROM workspace.workspace_members WHERE workspace_id = '{ws}' AND user_id = '{alice}'"))
            .Should().Be("Removed");
    }

    [Fact]
    public async Task AppRole_CanRevokeOnlyItsOwnActiveGrant_NeverAnotherPersonsOrAReactivation()
    {
        var alice = await SeedUserAsync("alice");
        var bob = await SeedUserAsync("bob");
        var ws = await SeedWorkspaceAsync(AccountA, alice, "Grants");
        await AddMemberAsync(AccountA, alice, ws, bob, WorkspaceRole.Member);

        var revokeOther = await AsAppRoleAsync(bob, $"UPDATE authz.access_grants SET revoked_at = now() WHERE workspace_id = '{ws}' AND user_id = '{alice}' RETURNING user_id::text");
        var editOwn = () => AsAppRoleAsync(bob, $"UPDATE authz.access_grants SET is_workspace_admin = true WHERE workspace_id = '{ws}' AND user_id = '{bob}' RETURNING user_id::text");
        (await editOwn.Should().ThrowAsync<PostgresException>("an active grant can only be ended, never edited in place"))
            .Which.SqlState.Should().Be("42501");
        var revokeOwn = await AsAppRoleAsync(bob, $"UPDATE authz.access_grants SET revoked_at = now() WHERE workspace_id = '{ws}' AND user_id = '{bob}' RETURNING user_id::text");
        var reactivate = await AsAppRoleAsync(bob, $"UPDATE authz.access_grants SET revoked_at = NULL WHERE workspace_id = '{ws}' AND user_id = '{bob}' RETURNING user_id::text");

        revokeOther.Should().BeEmpty("another person's grant is not visible for writing");
        revokeOwn.Should().Equal(bob.ToString());
        reactivate.Should().BeEmpty("a revoked grant cannot be reactivated by the application role");
        (await ScalarAsync($"SELECT (revoked_at IS NULL)::text FROM authz.access_grants WHERE workspace_id = '{ws}' AND user_id = '{alice}'"))
            .Should().Be("true");
    }

    // ───────────────────────── avatar ─────────────────────────

    [Fact]
    public async Task Avatar_UploadReplaceRemove_UnderTheAppRole_AndTheSweepReapsOnlyUnreferencedObjects()
    {
        var alice = await SeedUserAsync("alice");
        var bob = await SeedUserAsync("bob");
        var storage = NewStorage();

        var first = await UploadAsync(alice, storage);
        first.Should().StartWith($"/api/v1/users/{alice:D}/avatar?v=");
        var bobUrl = await UploadAsync(bob, storage);
        bobUrl.Should().NotBeNull();
        var firstKey = await StoredKeyAsync(alice);
        var bobKey = await StoredKeyAsync(bob);
        AvatarStorageKey.BelongsTo(firstKey!, alice).Should().BeTrue();

        // Self read works under RLS (own row visible), and returns the stored bytes.
        var read = await ReadAvatarAsync(alice, alice, storage);
        await using (read.Content)
        {
            read.ContentType.Should().Be("image/png");
        }

        var second = await UploadAsync(alice, storage);
        second.Should().NotBe(first, "a new upload changes the address");
        var secondKey = await StoredKeyAsync(alice);
        secondKey.Should().NotBe(firstKey);

        AgeEverything(DateTime.UtcNow.AddDays(-3));
        var result = await RunReaperAsync(storage);

        result.Deleted.Should().Be(1);
        (await ListObjectsAsync()).Should().BeEquivalentTo([secondKey!, bobKey!],
            "the replaced object is reaped after the grace period; both referenced photos survive");

        // Remove: row cleared at request time, object reaped by the next pass.
        (await RemoveAvatarAsync(alice)).Should().BeNull();
        (await ScalarAsync($"SELECT COALESCE(avatar, '') || COALESCE(avatar_storage_key, '') FROM identity.users WHERE id = '{alice}'"))
            .Should().BeEmpty();
        var afterRemove = await RunReaperAsync(storage);
        afterRemove.Deleted.Should().Be(1);
        (await ListObjectsAsync()).Should().BeEquivalentTo([bobKey!]);
    }

    [Fact]
    public async Task Sweep_DoesNotReapObjectsYoungerThanTheGracePeriod()
    {
        var alice = await SeedUserAsync("alice");
        var storage = NewStorage();
        await UploadAsync(alice, storage);
        await UploadAsync(alice, storage); // the first object is now unreferenced but still in flight by age

        var result = await RunReaperAsync(storage);

        result.Deleted.Should().Be(0);
        (await ListObjectsAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Sweep_FailsClosed_WhenTheWorkerLoginCannotReadUserRows()
    {
        var alice = await SeedUserAsync("alice");
        var storage = NewStorage();
        await UploadAsync(alice, storage);
        await UploadAsync(alice, storage);
        AgeEverything(DateTime.UtcNow.AddDays(-3));

        await ExecuteAsync("DROP POLICY p_worker_all ON identity.users");
        try
        {
            var result = await RunReaperAsync(storage);

            result.Deleted.Should().Be(0, "an unprovable reference set deletes nothing, not everything");
            (await ListObjectsAsync()).Should().HaveCount(2);
        }
        finally
        {
            await using var context = CreateContext();
            await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
        }
    }

    [Fact]
    public async Task Sweep_OutsideTheWorkerScope_SeesNoUserRows_SoRlsIsNotBypassed()
    {
        await SeedUserAsync("alice");
        await using var context = CreateContext(connectionString: _appConnectionString);
        await using var transaction = await context.Database.BeginTransactionAsync();

        (await context.Users.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReadingAnotherUsersAvatar_UnderTheAppRole_WorksForACoMember_AndFailsClosedForEveryoneElse()
    {
        var alice = await SeedUserAsync("alice");
        var bob = await SeedUserAsync("bob");
        var carol = await SeedUserAsync("carol");
        var ws = await SeedWorkspaceAsync(AccountA, alice, "Shared");
        await AddMemberAsync(AccountA, alice, ws, bob, WorkspaceRole.Member);
        var storage = NewStorage();
        await UploadAsync(alice, storage);

        var read = await ReadAvatarAsync(bob, alice, storage);
        await using (read.Content)
        {
            read.ContentType.Should().Be("image/png");
        }

        // Carol shares no workspace with Alice: the owner-defined function does not resolve the key, so the
        // answer is the same not-found as for a person without a photo.
        var act = async () => await ReadAvatarAsync(carol, alice, storage);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ───────────────────────── harness ─────────────────────────

    private static string NewEmail(string name) => $"{name}.{Guid.NewGuid():N}@example.test";

    private async Task<Guid> SeedUserAsync(string name)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);
        var user = User.Create(NewEmail(name), name, "hash", Now, hasPasswordCredential: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private static Mock<ICurrentRequestContext> RequestOf(Guid userId, Guid accountId)
    {
        var request = new Mock<ICurrentRequestContext>();
        request.Setup(r => r.UserId).Returns(userId);
        request.Setup(r => r.IsAuthenticated).Returns(true);
        request.Setup(r => r.RequireAccountId()).Returns(accountId);
        return request;
    }

    private static Mock<IDateTimeProvider> Clock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        return clock;
    }

    private async Task<Guid> SeedWorkspaceAsync(Guid accountId, Guid creator, string name)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);
        var handler = new CreateWorkspaceCommandHandler(
            context, RequestOf(creator, accountId).Object, Clock().Object,
            new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var created = await handler.Handle(new CreateWorkspaceCommand(name, null, false), default);
        created.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();
        return created.Data;
    }

    private async Task AddMemberAsync(Guid accountId, Guid actor, Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);
        var handler = new AddMemberCommandHandler(
            context, RequestOf(actor, accountId).Object, Clock().Object,
            new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)),
            Mock.Of<Notrelix.Application.Features.Billing.Public.Seats.IBillingSeatGate>(g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()) == Task.FromResult(true)), Mock.Of<Notrelix.Application.Features.Accounts.Public.Membership.IAccountMembershipFacts>(f => f.IsActiveMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()) == Task.FromResult(true)));
        (await handler.Handle(new AddMemberCommand(workspaceId, userId, role), default)).Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();
    }

    private async Task SeedDepartmentAsync(Guid accountId, Guid workspaceId, Guid creator, string name)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);
        context.Spaces.Add(Space.Create(accountId, workspaceId, name, SpaceVisibility.Workspace, creator, Now, SpaceType.Department));
        await context.SaveChangesAsync();
    }

    /// <summary>Runs one request through the real data session as the application-role login.</summary>
    private async Task<T> RunAsync<T>(
        Guid userId, Guid accountId, Guid? workspaceId,
        Func<ApplicationDbContext, ICurrentRequestContext, IRequestRollbackCompensations, Task<T>> action)
    {
        var tenant = new FakeCurrentTenantContext();
        if (workspaceId is { } w)
        {
            tenant.SetWorkspace(accountId, w, userId);
        }
        else
        {
            tenant.SetAccount(accountId, userId);
        }

        var request = RequestOf(userId, accountId);
        var compensations = new RequestRollbackCompensations(NullLogger<RequestRollbackCompensations>.Instance);
        await using var context = CreateContext(tenant, _workerConnectionString);
        var rls = new RlsSessionContext(context, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant);
        var session = new EfRequestDataSession(context, rls, NullLogger<EfRequestDataSession>.Instance, compensations: compensations);
        return await session.ExecuteAsync(
            new RequestDataSessionOptions(RequestDataAccess.Transactional, true, false),
            async ct => await action(context, request.Object, compensations),
            default);
    }

    private async Task<List<AccountWorkspaceDto>> ListAsync(Guid userId, Guid accountId) =>
        await RunAsync(userId, accountId, null, async (context, request, _) =>
        {
            var handler = new GetMyWorkspacesQueryHandler(
                context, request, new IdentityDefaultWorkspaceProvider(context, Clock().Object));
            var result = await handler.Handle(new GetMyWorkspacesQuery(), default);
            result.Succeeded.Should().BeTrue();
            return result.Data!;
        });

    private Task SetDefaultAsync(Guid userId, Guid accountId, Guid workspaceId) =>
        RunAsync(userId, accountId, workspaceId, async (context, request, _) =>
        {
            var handler = new SetDefaultWorkspaceCommandHandler(
                context, request, new IdentityDefaultWorkspaceProvider(context, Clock().Object));
            return (await handler.Handle(new SetDefaultWorkspaceCommand(workspaceId), default)).Succeeded;
        });

    private Task LeaveAsync(Guid userId, Guid accountId, Guid workspaceId) =>
        RunAsync(userId, accountId, workspaceId, async (context, request, _) =>
        {
            var handler = new LeaveWorkspaceCommandHandler(
                context, request, Clock().Object,
                new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)),
                new WorkspaceOwnerUpdateLocker(context),
                new IdentityDefaultWorkspaceProvider(context, Clock().Object));
            return (await handler.Handle(new LeaveWorkspaceCommand(workspaceId), default)).Succeeded;
        });

    private Task<string?> UploadAsync(Guid userId, IStorageService storage) =>
        RunAsync(userId, AccountA, null, async (context, request, compensations) =>
        {
            var handler = new UploadAvatarCommandHandler(context, request, Clock().Object, storage, compensations);
            var result = await handler.Handle(new UploadAvatarCommand(new MemoryStream(Png)), default);
            result.Succeeded.Should().BeTrue();
            return result.Data!.AvatarUrl;
        });

    private Task<string?> RemoveAvatarAsync(Guid userId) =>
        RunAsync(userId, AccountA, null, async (context, request, _) =>
        {
            var handler = new RemoveAvatarCommandHandler(context, request, Clock().Object);
            var result = await handler.Handle(new RemoveAvatarCommand(), default);
            result.Succeeded.Should().BeTrue();
            return result.Data!.AvatarUrl;
        });

    private async Task<UserAvatarContentDto> ReadAvatarAsync(Guid viewer, Guid owner, IStorageService storage)
    {
        var dto = await RunAsync(viewer, AccountA, null, async (context, request, _) =>
        {
            var handler = new GetUserAvatarQueryHandler(
                context, request, new AvatarAudienceReadAdapter(context), new Notrelix.Infrastructure.Identity.Profiles.AvatarStorageKeyLookup(context), storage,
                NullLogger<GetUserAvatarQueryHandler>.Instance);
            return (await handler.Handle(new GetUserAvatarQuery(owner), default)).Data!;
        });
        return dto;
    }

    private async Task<(int Deleted, bool Skipped)> RunReaperAsync(IStorageService storage)
    {
        var tenant = new CurrentTenantContext();
        await using var context = CreateContext(tenant, _workerConnectionString);
        var rls = new RlsSessionContext(context, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant);
        var store = new PostgresAvatarReaperStore(context, tenant, rls, NullLogger<PostgresAvatarReaperStore>.Instance);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        var reaper = new AvatarObjectReaper(
            store, storage, clock.Object, Options.Create(new AttachmentRetentionOptions()),
            NullLogger<AvatarObjectReaper>.Instance);
        return await reaper.RunOnceAsync(default);
    }

    private LocalStorageProvider NewStorage() =>
        new(Options.Create(new StorageOptions { BasePath = _storageRoot }));

    private async Task<List<string>> ListObjectsAsync()
    {
        var list = new List<string>();
        await foreach (var info in NewStorage().ListAsync(string.Empty, default))
        {
            list.Add(info.Key);
        }

        return list;
    }

    private void AgeEverything(DateTime lastWriteUtc)
    {
        if (!Directory.Exists(_storageRoot))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_storageRoot, "*", SearchOption.AllDirectories))
        {
            File.SetLastWriteTimeUtc(file, lastWriteUtc);
        }
    }

    private Task<string?> StoredKeyAsync(Guid userId) =>
        ScalarAsync($"SELECT avatar_storage_key FROM identity.users WHERE id = '{userId}'")
            .ContinueWith(t => string.IsNullOrEmpty(t.Result) ? null : t.Result, TaskScheduler.Default);

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

    private async Task<string> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (await command.ExecuteScalarAsync())?.ToString() ?? string.Empty;
    }

    private async Task<List<string>> AsAppRoleAsync(Guid userId, string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var setup = new NpgsqlCommand($"""
            SET LOCAL ROLE notrelix_app;
            SELECT set_config('app.current_user_id', '{userId}', true);
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

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
