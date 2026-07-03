using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Testing.Integration.Factories;

internal sealed class TestDbContext : ApplicationDbContext
{
    public TestDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext? tenant = null)
        : base(options, tenant)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SearchDocumentRecord>().Ignore(x => x.SearchVector);
    }
}

public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext(params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"Notrelix-test-{Guid.NewGuid():N}")
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>();

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new TestDbContext(optionsBuilder.Options);
    }

    public static ApplicationDbContext CreateInMemoryContext(ICurrentTenantContext tenant, params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"Notrelix-test-{Guid.NewGuid():N}")
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>();

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new TestDbContext(optionsBuilder.Options, tenant);
    }
}
