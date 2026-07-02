using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Abstractions.Rls;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Resiliency;

[Collection("Database")]
public class MigrationResiliencyTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public MigrationResiliencyTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SeedAsync_WhenAlreadySeeded_IsIdempotent()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var initialiser = CreateInitialiser(context);

        await initialiser.SeedAsync();
        var firstCount = await context.Users.CountAsync();

        await initialiser.SeedAsync();
        var secondCount = await context.Users.CountAsync();

        secondCount.Should().Be(firstCount);
    }

    private static ApplicationDbContextInitialiser CreateInitialiser(ApplicationDbContext context)
    {
        return new ApplicationDbContextInitialiser(
            NullLogger<ApplicationDbContextInitialiser>.Instance,
            context,
            new DeterministicPasswordHasher(),
            Options.Create(new SeedDataOptions
            {
                Enabled = true,
                Profile = SeedProfile.Small,
                ResetBeforeSeed = false
            }),
            new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance),
            new FakeCurrentWorkspace(),
            Options.Create(new RlsOptions()));
    }

    private sealed class DeterministicPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => "hashed-" + password;
        public bool VerifyPassword(string password, string hashedPassword) => hashedPassword == HashPassword(password);
    }
}
