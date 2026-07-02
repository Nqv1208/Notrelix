using Notrelix.Application.Common.Abstractions;

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

            return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeCurrentWorkspace());
        }

        private sealed class DesignTimeCurrentWorkspace : ICurrentWorkspace
        {
            public Guid? AccountId => null;
            public Guid? WorkspaceId => null;
            public bool IsSet => false;
            public bool IsSystemContext => true;
            public void SetWorkspace(Guid accountId, Guid workspaceId) { }
            public IDisposable EnterSystemContext() => new NoopDisposable();
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}