using System.Reflection;

namespace Notrelix.Architecture.Tests;

public class DependencyArchitectureTests
{
    private static Assembly GetAssembly<T>() => typeof(T).Assembly;

    private static HashSet<string> GetReferencedAssemblyNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Domain_ShouldNotReference_Application()
    {
        var domain = GetAssembly<Notrelix.Domain.Common.Entity>();
        var refs = GetReferencedAssemblyNames(domain);

        refs.Should().NotContain("Notrelix.Application",
            "Domain must not depend on Application layer");
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        var domain = GetAssembly<Notrelix.Domain.Common.Entity>();
        var refs = GetReferencedAssemblyNames(domain);

        refs.Should().NotContain("Notrelix.Infrastructure",
            "Domain must not depend on Infrastructure layer");
    }

    [Fact]
    public void Domain_ShouldNotReference_Api()
    {
        var domain = GetAssembly<Notrelix.Domain.Common.Entity>();
        var refs = GetReferencedAssemblyNames(domain);

        refs.Should().NotContain("Notrelix.API",
            "Domain must not depend on API layer");
    }

    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        var app = GetAssembly<Notrelix.Application.Common.Abstractions.ICurrentWorkspace>();
        var refs = GetReferencedAssemblyNames(app);

        refs.Should().NotContain("Notrelix.Infrastructure",
            "Application must not depend on Infrastructure layer");
    }

    [Fact]
    public void Application_ShouldNotReference_Api()
    {
        var app = GetAssembly<Notrelix.Application.Common.Abstractions.ICurrentWorkspace>();
        var refs = GetReferencedAssemblyNames(app);

        refs.Should().NotContain("Notrelix.API",
            "Application must not depend on API layer");
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_Api()
    {
        var infra = GetAssembly<Notrelix.Infrastructure.Data.ApplicationDbContext>();
        var refs = GetReferencedAssemblyNames(infra);

        refs.Should().NotContain("Notrelix.API",
            "Infrastructure must not depend on API layer");
    }

    [Fact]
    public void Api_ShouldReference_Application()
    {
        var api = GetAssembly<Notrelix.API.Program>();
        var refs = GetReferencedAssemblyNames(api);

        refs.Should().Contain("Notrelix.Application",
            "API must reference Application layer for use case orchestration");
    }

    [Fact]
    public void Api_ShouldReference_Infrastructure()
    {
        var api = GetAssembly<Notrelix.API.Program>();
        var refs = GetReferencedAssemblyNames(api);

        refs.Should().Contain("Notrelix.Infrastructure",
            "API must reference Infrastructure layer for composition root/DI registration");
    }
}
