using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateAccountPreferences;
using Notrelix.Application.Features.Identity.Profiles.DTOs;
using Notrelix.Application.Features.Identity.Profiles.Queries.GetAccountPreferences;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// Account preferences against the real PostgreSQL schema: first save creates the row, the
/// version is the concurrency token (stale expectedVersion rejected, a lost race rejected by the
/// database), and one user can never read or change another user's preferences (NRX-003).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class AccountPreferencesPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private User _alice = null!;
    private User _bob = null!;

    public AccountPreferencesPersistenceTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        _alice = User.Create("alice-prefs@example.com", "Alice", "hashed", Now.AddDays(-1), hasPasswordCredential: true);
        _bob = User.Create("bob-prefs@example.com", "Bob", "hashed", Now.AddDays(-1), hasPasswordCredential: true);
        await using var context = _db.CreateOwnerContext(SystemTenant());
        context.Users.AddRange(_alice, _bob);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private async Task<Notrelix.Application.Common.Models.Result<AccountPreferencesDto>> UpdateAsync(User actor, UpdateAccountPreferencesCommand command)
    {
        var tenant = SystemTenant();
        var context = _db.CreateOwnerContext(tenant);
        var session = new EfRequestDataSession(
            context,
            new RlsSessionContext(context, Options.Create(new RlsOptions()), tenant),
            NullLogger<EfRequestDataSession>.Instance);
        var time = new Mock<IDateTimeProvider>();
        time.Setup(t => t.UtcNow).Returns(Now);
        var handler = new UpdateAccountPreferencesCommandHandler(
            context, new FakeCurrentRequestContext().AsUser(actor.Id), time.Object);

        try
        {
            return await session.ExecuteAsync(
                new RequestDataSessionOptions(RequestDataAccess.Transactional, ApplyTenantScope: false, ApplyResourceScope: false, ExpectedVersion: null),
                ct => handler.Handle(command, ct),
                CancellationToken.None);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    private async Task<AccountPreferencesDto> ReadAsync(User actor)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var handler = new GetAccountPreferencesQueryHandler(context, new FakeCurrentRequestContext().AsUser(actor.Id));
        return (await handler.Handle(new GetAccountPreferencesQuery(), CancellationToken.None)).Data!;
    }

    [Fact]
    public async Task Get_BeforeTheFirstSave_ReturnsDefaultsAtVersionZeroAndCreatesNoRow()
    {
        (await ReadAsync(_alice)).Should().Be(AccountPreferencesDto.Defaults);

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.UserProfiles.CountAsync()).Should().Be(0, "a read must not write");
    }

    [Fact]
    public async Task FirstSave_CreatesTheRow_AndTheNextReadReturnsItsVersion()
    {
        var saved = await UpdateAsync(_alice, new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = 0,
            Locale = "en",
            Timezone = "Asia/Tokyo",
            Theme = "dark"
        });

        saved.Succeeded.Should().BeTrue();
        var read = await ReadAsync(_alice);
        read.Should().Be(saved.Data!);
        read.Should().Match<AccountPreferencesDto>(d => d.Locale == "en" && d.Timezone == "Asia/Tokyo" && d.Theme == "dark" && d.Version > 0);
    }

    [Fact]
    public async Task SecondSave_WithTheReadVersion_Succeeds_AndAStaleVersionIsRejectedWithoutChange()
    {
        var first = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = 0, Theme = "dark" })).Data!;
        var second = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = first.Version, Theme = "light" })).Data!;
        second.Version.Should().BeGreaterThan(first.Version);

        var stale = async () => await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = first.Version, Theme = "system" });

        var thrown = (await stale.Should().ThrowAsync<PreconditionFailedException>()).Which;
        thrown.CurrentVersion.Should().Be(second.Version);
        (await ReadAsync(_alice)).Should().Be(second, "a rejected write leaves state and version unchanged");
    }

    [Fact]
    public async Task ClaimingAVersionBeforeTheFirstSave_IsRejectedAndCreatesNoRow()
    {
        var act = async () => await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = 4, Theme = "dark" });

        await act.Should().ThrowAsync<PreconditionFailedException>();
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.UserProfiles.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LostRace_BetweenTwoWritersOfTheSameVersion_IsRejectedByTheDatabase()
    {
        var first = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = 0, Theme = "dark" })).Data!;

        // Writer B loaded the profile at `first.Version`, then writer A commits, then B saves.
        await using var writerB = _db.CreateOwnerContext(SystemTenant());
        var staleCopy = await writerB.UserProfiles.SingleAsync(p => p.UserId == _alice.Id);
        staleCopy.Version.Should().Be(first.Version);

        var winner = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = first.Version, Theme = "light" })).Data!;

        staleCopy.ApplyAccountPreferences(null, "Asia/Tokyo", null, Now.AddMinutes(1));
        var loser = async () => await writerB.SaveChangesAsync();

        await loser.Should().ThrowAsync<DbUpdateConcurrencyException>();
        (await ReadAsync(_alice)).Should().Be(winner);
    }

    [Fact]
    public async Task OneUsersPreferences_AreNeverVisibleToOrChangedByAnotherUser()
    {
        var aliceSaved = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = 0, Locale = "en", Theme = "dark" })).Data!;

        (await ReadAsync(_bob)).Should().Be(AccountPreferencesDto.Defaults);

        var bobSaved = (await UpdateAsync(_bob, new UpdateAccountPreferencesCommand { ExpectedVersion = 0, Theme = "light" })).Data!;
        bobSaved.Locale.Should().Be("vi");
        (await ReadAsync(_alice)).Should().Be(aliceSaved);
    }

    [Fact]
    public async Task SemanticNoOp_DoesNotAdvanceTheStoredVersion()
    {
        var first = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = 0, Theme = "dark" })).Data!;

        var again = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = first.Version, Theme = "dark" })).Data!;

        again.Version.Should().Be(first.Version);
        (await ReadAsync(_alice)).Version.Should().Be(first.Version);
    }
    [Fact]
    public async Task Phone_DefaultsToSelfOnly_IsStoredNormalized_AndVisibilityIsAnExplicitOptIn()
    {
        (await ReadAsync(_alice)).Should().Match<AccountPreferencesDto>(d => d.Phone == null && !d.PhoneVisibleToWorkspaceMembers);

        var first = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = 0, Phone = " +84 90-123.4567 " })).Data!;
        first.Phone.Should().Be("+84901234567");
        first.PhoneVisibleToWorkspaceMembers.Should().BeFalse("saving a phone does not share it");

        var shared = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = first.Version, PhoneVisibleToWorkspaceMembers = true })).Data!;
        shared.Phone.Should().Be("+84901234567", "toggling the flag keeps the phone");
        (await ReadAsync(_alice)).Should().Be(shared);

        var cleared = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = shared.Version, Phone = "" })).Data!;
        cleared.Phone.Should().BeNull();
        cleared.PhoneVisibleToWorkspaceMembers.Should().BeTrue("clearing the phone does not touch the flag");
    }

    [Fact]
    public async Task InvalidPhone_IsRejected_AndLeavesTheStoredPreferencesUnchanged()
    {
        var first = (await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = 0, Phone = "+84901234567" })).Data!;

        var bad = async () => await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = first.Version, Theme = "dark", Phone = "0901234567" });

        await bad.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        (await ReadAsync(_alice)).Should().Be(first, "an invalid phone rejects the whole write");
    }

    [Fact]
    public async Task OneUsersPhone_IsNeverReadableByAnotherUser_ThroughTheProfileRead()
    {
        await UpdateAsync(_alice, new UpdateAccountPreferencesCommand { ExpectedVersion = 0, Phone = "+84901234567", PhoneVisibleToWorkspaceMembers = true });

        var bobView = await ReadAsync(_bob);

        bobView.Phone.Should().BeNull("the profile read is always the principal's own, even when the phone is shared with workspace members");
        bobView.PhoneVisibleToWorkspaceMembers.Should().BeFalse();
    }
}
