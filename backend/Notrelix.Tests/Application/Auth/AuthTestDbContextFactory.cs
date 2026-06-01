using Microsoft.EntityFrameworkCore;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Tests.Application.Auth;

public static class AuthTestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"Notrelix-auth-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}

