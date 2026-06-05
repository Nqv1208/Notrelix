using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Application.Tests.Auth;

public static class AuthTestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext(params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"Notrelix-auth-{Guid.NewGuid():N}");

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
