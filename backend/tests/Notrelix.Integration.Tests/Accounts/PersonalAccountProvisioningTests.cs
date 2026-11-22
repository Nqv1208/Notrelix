using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Accounts;

/// <summary>
/// The personal Account a registration provisions, against a real PostgreSQL: people who register at the same
/// moment under the same display name all succeed with their own slug, a long display name does not fail the
/// registration, and the unique index (not the generator) is what makes a slug name one Account.
/// </summary>
[Collection("Database")]
public class PersonalAccountProvisioningTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public PersonalAccountProvisioningTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EightRegistrationsAtOnceUnderTheSameDisplayName_AllCommit_EachWithItsOwnSlugAndOwner()
    {
        const int people = 8;
        var contexts = Enumerable.Range(0, people).Select(_ => _db.CreateOwnerContext()).ToList();
        try
        {
            await Task.WhenAll(contexts.Select((context, i) => CreateHandler(context).Handle(
                new RegisterCommand { Email = $"same-name-{i}@example.com", Password = "Password1!", Name = "Minh An" },
                CancellationToken.None)));
            await Task.WhenAll(contexts.Select(context => context.SaveChangesAsync()));
        }
        finally
        {
            foreach (var context in contexts)
                await context.DisposeAsync();
        }

        await using var read = _db.CreateOwnerContext();
        var accounts = await read.Accounts.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        accounts.Should().HaveCount(people, "one personal Account per registration");
        accounts.Select(a => a.Slug).Distinct().Should().HaveCount(people, "no two of them share a slug");
        accounts.Should().OnlyContain(a => a.Type == AccountType.Personal && a.Status == AccountStatus.Active);
        (await read.Users.IgnoreQueryFilters().CountAsync()).Should().Be(people);
        (await read.AccountMembers.IgnoreQueryFilters().CountAsync(m => m.Role == AccountRole.Owner))
            .Should().Be(people, "each person owns exactly their own Account");
    }

    [Fact]
    public async Task ADisplayNameOfTheLongestAllowedLength_StillRegisters()
    {
        await using var context = _db.CreateOwnerContext();

        await CreateHandler(context).Handle(
            new RegisterCommand
            {
                Email = "long-name@example.com",
                Password = "Password1!",
                Name = new string('x', User.MaxNameLength),
            },
            CancellationToken.None);
        await context.SaveChangesAsync();

        await using var read = _db.CreateOwnerContext();
        var account = await read.Accounts.IgnoreQueryFilters().AsNoTracking().SingleAsync();
        account.Name.Length.Should().BeLessThanOrEqualTo(160);
        account.Slug.Length.Should().BeLessThanOrEqualTo(128);
    }

    [Fact]
    public async Task ARegistrationRetriedWithTheSameEmail_HasNoSecondEffect()
    {
        var command = new RegisterCommand { Email = "retry@example.com", Password = "Password1!", Name = "Retry" };
        await using (var first = _db.CreateOwnerContext())
        {
            await CreateHandler(first).Handle(command, CancellationToken.None);
            await first.SaveChangesAsync();
        }

        await using (var retry = _db.CreateOwnerContext())
        {
            await CreateHandler(retry).Handle(command, CancellationToken.None);
            await retry.SaveChangesAsync();
        }

        await using var read = _db.CreateOwnerContext();
        (await read.Users.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await read.Accounts.IgnoreQueryFilters().CountAsync()).Should().Be(1, "the retry provisions nothing");
        (await read.AccountMembers.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task TheUniqueIndex_RejectsASecondAccountWithTheSameSlug_AndNamesTheIndex()
    {
        var creator = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var first = _db.CreateOwnerContext())
        {
            first.Accounts.Add(Account.Create("One", "taken-slug", AccountType.Team, creator, now));
            await first.SaveChangesAsync();
        }

        await using var second = _db.CreateOwnerContext();
        second.Accounts.Add(Account.Create("Two", "taken-slug", AccountType.Team, creator, now));

        var act = () => second.SaveChangesAsync();

        var failure = await act.Should().ThrowAsync<DbUpdateException>();
        var postgres = failure.Which.InnerException.Should().BeOfType<PostgresException>().Subject;
        postgres.SqlState.Should().Be("23505");
        postgres.ConstraintName.Should().Be("idx_account_slug");
        await using var read = _db.CreateOwnerContext();
        (await read.Accounts.IgnoreQueryFilters().CountAsync(a => a.Slug == "taken-slug"))
            .Should().Be(1, "the losing write leaves no row");
    }

    private static RegisterCommandHandler CreateHandler(ApplicationDbContext context)
    {
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");

        return new RegisterCommandHandler(
            context,
            new AccountProvisioningService(context, new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context))),
            passwordHasher.Object,
            dateTimeProvider.Object,
            new Mock<IIntegrationEventCollector>().Object,
            Mock.Of<Notrelix.Application.Common.RateLimiting.IRateLimitService>());
    }
}
