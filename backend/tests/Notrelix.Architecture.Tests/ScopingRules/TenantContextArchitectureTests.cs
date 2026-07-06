using System.Reflection;

namespace Notrelix.Architecture.Tests;

/// <summary>
/// Enforces tenant context consistency across the codebase.
/// ICurrentTenantContext is the single source of truth.
/// </summary>
public class TenantContextArchitectureTests
{
    private static readonly Assembly InfrastructureAssembly =
        typeof(Notrelix.Infrastructure.Data.ApplicationDbContext).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(Notrelix.Application.Common.Context.ICurrentTenantContext).Assembly;

    [Fact]
    public void ApplicationDbContext_ShouldNotInject_ICurrentWorkspace()
    {
        var type = typeof(Notrelix.Infrastructure.Data.ApplicationDbContext);
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var ctor in ctors)
        {
            foreach (var param in ctor.GetParameters())
            {
                param.ParameterType.Name.Should().NotBe("ICurrentWorkspace",
                    "ApplicationDbContext must use ICurrentTenantContext, not ICurrentWorkspace");
            }
        }
    }

    [Fact]
    public void RlsSessionContext_ShouldNotInject_ICurrentWorkspace_Or_ICurrentAccount()
    {
        var type = InfrastructureAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "RlsSessionContext");

        type.Should().NotBeNull("RlsSessionContext should exist");

        var ctors = type!.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var ctor in ctors)
        {
            foreach (var param in ctor.GetParameters())
            {
                param.ParameterType.Name.Should().NotBe("ICurrentWorkspace",
                    "RlsSessionContext must use ICurrentTenantContext");
                param.ParameterType.Name.Should().NotBe("ICurrentAccount",
                    "RlsSessionContext must use ICurrentTenantContext");
            }
        }
    }

    [Fact]
    public void DesignTimeFactory_ShouldNotUse_ICurrentWorkspace()
    {
        var type = InfrastructureAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ApplicationDbContextFactory");

        type.Should().NotBeNull();

        var source = File.ReadAllText(
            Path.Combine(FindProjectRoot(), "src", "Notrelix.Infrastructure", "Data", "ApplicationDbContextFactory.cs"));

        source.Should().NotContain("ICurrentWorkspace",
            "Design-time factory must use ICurrentTenantContext");
        source.Should().NotContain("DesignTimeCurrentWorkspace",
            "Design-time factory must use DesignTimeTenantContext");
    }

    [Fact]
    public void Initialiser_ShouldNotUse_ICurrentWorkspace()
    {
        var source = File.ReadAllText(
            Path.Combine(FindProjectRoot(), "src", "Notrelix.Infrastructure", "Data", "ApplicationDbContextInitialiser.cs"));

        source.Should().NotContain("ICurrentWorkspace",
            "ApplicationDbContextInitialiser must use ICurrentTenantContext");
        source.Should().NotContain("EnterSystemContext",
            "ApplicationDbContextInitialiser must use _tenant.SetSystem()");
    }

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null && !File.Exists(Path.Combine(dir, "backend.slnx")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir ?? throw new InvalidOperationException("Could not find project root");
    }
}