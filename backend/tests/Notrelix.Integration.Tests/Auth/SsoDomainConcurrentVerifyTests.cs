using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Sso;
using Notrelix.Application.Features.Accounts.Sso.Commands.VerifySsoDomain;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>Two Accounts proving the same domain at the same moment: the partial unique index lets one win and the loser gets accounts.domain.claimed.</summary>
[Collection("Database")]
public class SsoDomainConcurrentVerifyTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;

    public SsoDomainConcurrentVerifyTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed class RacingResolver(string txt, Func<Task> otherAccountWins) : IDomainTxtResolver
    {
        public async Task<IReadOnlyList<string>> ResolveTxtAsync(string domain, CancellationToken cancellationToken)
        {
            await otherAccountWins();
            return [txt];
        }
    }

    [Fact]
    public async Task TheLoserOfAConcurrentDoubleVerify_GetsTheClaimedRefusal_NotAGenericDatabaseError()
    {
        var encryptor = new ReversibleSecretEncryptor();
        var now = DateTimeOffset.UtcNow;
        var owner = Guid.CreateVersion7();
        var accountA = Account.Create("A Corp", "a-corp", AccountType.Team, owner, now);
        var accountB = Account.Create("B Corp", "b-corp", AccountType.Team, owner, now);
        var token = AccountSsoSupport.NewDomainToken();
        var rowA = AccountDomain.Create(accountA.Id, "acme.com", owner, now, AccountSsoSupport.HashToken(token), encryptor.Protect(token, AccountSsoSupport.DomainTokenPurpose));
        var rowB = AccountDomain.Create(accountB.Id, "acme.com", owner, now, AccountSsoSupport.HashToken("b"), encryptor.Protect("b", AccountSsoSupport.DomainTokenPurpose));
        await using (var seed = _db.CreateOwnerContext())
        {
            seed.Accounts.AddRange(accountA, accountB);
            seed.AccountDomains.AddRange(rowA, rowB);
            await seed.SaveChangesAsync();
        }

        await using var context = _db.CreateOwnerContext();
        var request = new FakeCurrentRequestContext().AsUser(owner);
        request.Tenant.SetAccount(accountA.Id, owner);
        var resolver = new RacingResolver(AccountSsoSupport.TxtValue(token), async () =>
        {
            // Account B's Owner verifies the same domain between A's claimed check and A's save.
            await using var other = _db.CreateOwnerContext();
            var b = await other.AccountDomains.SingleAsync(d => d.Id == rowB.Id);
            b.Verify(DateTimeOffset.UtcNow, owner);
            await other.SaveChangesAsync();
        });
        var handler = new VerifySsoDomainCommandHandler(
            context, request, FakeDateTimeProvider.WithFixedTime(now), encryptor, resolver, new SsoDomainClaims(context));

        var loser = await FluentActions.Invoking(() => handler.Handle(new VerifySsoDomainCommand(rowA.Id), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();

        loser.Which.ErrorCode.Should().Be(AccountSsoErrorCodes.DomainClaimed);
        await using var check = _db.CreateOwnerContext();
        (await check.AccountDomains.SingleAsync(d => d.Id == rowA.Id)).VerificationStatus.Should().Be(DomainVerificationStatus.Pending);
        (await check.AccountDomains.SingleAsync(d => d.Id == rowB.Id)).VerificationStatus.Should().Be(DomainVerificationStatus.Verified);
    }
}
