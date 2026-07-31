using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

/// <summary>
/// Ensures Domain does not expose internals to production assemblies.
/// Only Domain.Tests may access internal members for white-box testing.
/// </summary>
public class DomainFriendAssemblyTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    [Fact]
    public void Domain_must_not_expose_internals_to_production_assemblies()
    {
        var allowedAssemblies = new HashSet<string>
        {
            "Notrelix.Domain.Tests"
        };

        var actualAssemblies = DomainAssembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(attribute => attribute.AssemblyName.Split(',')[0])
            .ToArray();

        actualAssemblies.Should()
            .OnlyContain(name => allowedAssemblies.Contains(name),
                "Domain must not expose internals to production assemblies. " +
                "Application, Infrastructure, and API must use public Domain contract only.");
    }

    [Fact]
    public void Domain_must_not_expose_internals_to_Application()
    {
        var friendAssemblies = DomainAssembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(attribute => attribute.AssemblyName.Split(',')[0])
            .ToArray();

        friendAssemblies.Should()
            .NotContain("Notrelix.Application",
                "Application must use public Domain contract, not internal implementation details.");
    }

    [Fact]
    public void Domain_must_not_expose_internals_to_Infrastructure()
    {
        var friendAssemblies = DomainAssembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(attribute => attribute.AssemblyName.Split(',')[0])
            .ToArray();

        friendAssemblies.Should()
            .NotContain("Notrelix.Infrastructure",
                "Infrastructure must use public Domain contract and EF materialization, not internal access.");
    }

    [Fact]
    public void Domain_must_not_expose_internals_to_API()
    {
        var friendAssemblies = DomainAssembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(attribute => attribute.AssemblyName.Split(',')[0])
            .ToArray();

        friendAssemblies.Should()
            .NotContain("Notrelix.API",
                "API must use Application layer, not Domain internals directly.");
    }
}
