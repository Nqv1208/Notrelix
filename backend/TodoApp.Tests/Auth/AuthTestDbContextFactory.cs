using Microsoft.EntityFrameworkCore;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Tests.Auth;

public static class AuthTestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"todoapp-auth-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}

