namespace Notrelix.Architecture.Tests;

/// <summary>
/// Guards against SQLite reintroduction into Notrelix test infrastructure.
/// Notrelix production uses PostgreSQL. All database tests must use PostgreSQL
/// to ensure test behavior matches production.
/// </summary>
public class DatabaseProviderArchitectureTests
{
    [Fact]
    public void Production_Assemblies_ShouldNotReference_Sqlite()
    {
        var assemblies = new[]
        {
            typeof(Notrelix.Domain.Common.AggregateRoot).Assembly,
            typeof(Notrelix.Infrastructure.Data.ApplicationDbContext).Assembly,
            typeof(Notrelix.API.Program).Assembly
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies();
            var sqliteRefs = references.Where(a =>
                (a.Name ?? "").Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ||
                (a.Name ?? "").Contains("SQLitePCLRaw", StringComparison.OrdinalIgnoreCase));

            sqliteRefs.Should().BeEmpty(
                $"Production assembly '{assembly.GetName().Name}' must not reference SQLite. " +
                "Use PostgreSQL (Npgsql) instead.");
        }
    }

    [Fact]
    public void Domain_ShouldNotReference_EntityFrameworkCore()
    {
        var domainAssembly = typeof(Notrelix.Domain.Common.AggregateRoot).Assembly;
        var references = domainAssembly.GetReferencedAssemblies();

        references.Should().NotContain(a => (a.Name ?? "").StartsWith("Microsoft.EntityFrameworkCore"),
            "Domain layer must be pure and must not reference EF Core");
    }
}
