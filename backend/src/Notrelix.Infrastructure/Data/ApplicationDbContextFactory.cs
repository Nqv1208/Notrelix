
namespace Notrelix.Infrastructure.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseNpgsql(
                "Host=127.0.0.1;Port=5432;Database=notrelix_dev;Username=postgres;Password=postgres"
            ).UseSnakeCaseNamingConvention();

            return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
        }

        private sealed class DesignTimeTenantContext : ICurrentTenantContext
        {
            public Guid? AccountId => null;
            public Guid? WorkspaceId => null;
            public Guid? UserId => null;
            public bool IsSystemContext => true;
            public bool IsResolved => true;
            public Guid RequireAccountId() => throw new InvalidOperationException("Design-time only.");
            public Guid RequireWorkspaceId() => throw new InvalidOperationException("Design-time only.");
            public Guid RequireUserId() => throw new InvalidOperationException("Design-time only.");
            public void SetAccount(Guid accountId, Guid? userId) { }
            public void SetWorkspace(Guid accountId, Guid workspaceId, Guid? userId) { }
            public void SetSystem() { }
            public void Clear() { }
        }
    }
}