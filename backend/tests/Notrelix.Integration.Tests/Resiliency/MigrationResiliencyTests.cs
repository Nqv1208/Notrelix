using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Resiliency;

public class MigrationResiliencyTests
{
    [Fact]
    public async Task SeedAsync_WhenAlreadySeeded_IsIdempotent()
    {
        await using var context = CreateContext();
        var initialiser = CreateInitialiser(context);

        await initialiser.SeedAsync();
        var firstCount = await context.Users.CountAsync();

        await initialiser.SeedAsync();
        var secondCount = await context.Users.CountAsync();

        secondCount.Should().Be(firstCount);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-migration-{Guid.NewGuid():N}")
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>()
            .Options;
        return new TestApplicationDbContext(options);
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
            new FakeCurrentWorkspace());
    }

    private sealed class DeterministicPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => "hashed-" + password;
        public bool VerifyPassword(string password, string hashedPassword) => hashedPassword == HashPassword(password);
    }

    private sealed class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    }
}
