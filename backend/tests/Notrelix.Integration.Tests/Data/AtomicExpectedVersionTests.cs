using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Data.Rls;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Data;

[Collection("Database")]
[Trait("Category", "RequiresDocker")]
public sealed class AtomicExpectedVersionTests
{
    private readonly PostgresTestContainer _db;

    public AtomicExpectedVersionTests(PostgresTestContainer db)
    {
        _db = db;
    }

    [Fact]
    public async Task ConcurrentWriters_SecondCommitWithStaleVersion_ShouldFailAtomically()
    {
        var accountId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(
            accountId,
            actorId,
            "Concurrency workspace",
            $"concurrency-{Guid.NewGuid():N}",
            now);

        await using (var seed = _db.CreateContext())
        {
            seed.Workspaces.Add(workspace);
            await seed.SaveChangesAsync();
        }

        await using var firstContext = _db.CreateContext();
        await using var secondContext = _db.CreateContext();
        var first = await firstContext.Workspaces.IgnoreQueryFilters().SingleAsync(x => x.Id == workspace.Id);
        var second = await secondContext.Workspaces.IgnoreQueryFilters().SingleAsync(x => x.Id == workspace.Id);
        var expectedVersion = first.Version;

        var firstSession = CreateSession(firstContext);
        await firstSession.ExecuteAsync(
            Options(expectedVersion, workspace.Id),
            _ =>
            {
                first.Rename("First writer", actorId, now.AddSeconds(1));
                return Task.FromResult(true);
            },
            CancellationToken.None);

        var secondSession = CreateSession(secondContext);
        var staleWrite = () => secondSession.ExecuteAsync(
            Options(expectedVersion, workspace.Id),
            _ =>
            {
                second.Rename("Second writer", actorId, now.AddSeconds(2));
                return Task.FromResult(true);
            },
            CancellationToken.None);

        await staleWrite.Should().ThrowAsync<DbUpdateConcurrencyException>();

        await using var verify = _db.CreateContext();
        var persisted = await verify.Workspaces.IgnoreQueryFilters().SingleAsync(x => x.Id == workspace.Id);
        persisted.Name.Should().Be("First writer");
        persisted.Version.Should().Be(expectedVersion + 1);
    }

    private static EfRequestDataSession CreateSession(ApplicationDbContext context) =>
        new(context, Mock.Of<IRlsSessionContext>(), NullLogger<EfRequestDataSession>.Instance);

    private static RequestDataSessionOptions Options(long expectedVersion, Guid resourceId) =>
        new(
            RequestDataAccess.Transactional,
            ApplyTenantScope: false,
            ApplyResourceScope: false,
            new ExpectedVersionConstraint(resourceId, expectedVersion));
}
