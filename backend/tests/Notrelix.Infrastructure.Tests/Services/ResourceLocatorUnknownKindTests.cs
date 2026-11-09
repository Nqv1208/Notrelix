using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Context;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Services;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Services;

/// <summary>
/// UC-COL-B18: a target kind is accepted only when the locator knows it. A kind with no contract resolves to no
/// resource, so every route that locates a target fails closed (NotFound) and never trusts a naked id.
/// </summary>
public sealed class ResourceLocatorUnknownKindTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Author = Guid.NewGuid();

    [Fact]
    public async Task UnregisteredKind_ResolvesToNothing_EvenForAnExistingId()
    {
        var context = CreateContext();
        var comment = Comment.Create(Account, Workspace,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), Workspace),
            "Hello", Author, Now);
        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        var locator = new ResourceLocator(context, context, context, context, context, context,
            new Mock<IRlsSessionContext>().Object);

        var located = await locator.LocateAsync(
            ResourceRef.Create(ResourceKind.Create("collaboration.unregistered-target"), comment.Id, Workspace),
            Author, CancellationToken.None);

        located.Should().BeNull("a kind without a contract is refused, whatever the id");
    }

    [Fact]
    public async Task RegisteredKind_ResolvesTheSameComment()
    {
        var context = CreateContext();
        var comment = Comment.Create(Account, Workspace,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), Workspace),
            "Hello", Author, Now);
        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        var locator = new ResourceLocator(context, context, context, context, context, context,
            new Mock<IRlsSessionContext>().Object);

        var located = await locator.LocateAsync(
            ResourceRef.Create(ResourceKind.Create("collaboration.comment"), comment.Id, Workspace),
            Author, CancellationToken.None);

        located.Should().NotBeNull();
        located!.ResourceKind.Value.Should().Be("collaboration.comment");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return new LocatorTestContext(options, tenant);
    }

    private sealed class LocatorTestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }
}
