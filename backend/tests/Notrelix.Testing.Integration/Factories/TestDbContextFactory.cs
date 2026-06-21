using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Testing.Integration.Factories;

public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext(params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"Notrelix-test-{Guid.NewGuid():N}");

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
