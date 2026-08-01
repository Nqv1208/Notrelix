using System.Reflection;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests;

public class DomainFrameworkDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly string[] ForbiddenNamespaces =
    [
        "System.Data",
        "System.Data.Entity",
        "System.Xml",
        "System.Net.Http",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Extensions",
        "StackExchange.Redis",
        "Npgsql",
        "FluentValidation",
        "MediatR",
        "AutoMapper",
        "Swashbuckle",
    ];

    private static readonly string[] ForbiddenTypeNames =
    [
        "DbContext",
        "DbContextOptions",
    ];

    [Fact]
    public void Domain_ShouldNotReferenceForbiddenNamespaces()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;

            var referencedTypes = DomainTypeGraphWalker.GetReferencedTypes(type);

            foreach (var referencedType in referencedTypes)
            {
                var ns = referencedType.Namespace;
                if (ns is not null && ForbiddenNamespaces.Contains(ns))
                    violations.Add($"{type.FullName} -> {ns}");
            }
        }

        violations.Should().BeEmpty(
            "domain types must not reference forbidden infrastructure namespaces: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void Domain_ShouldNotReferenceForbiddenTypes()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;

            var referencedTypes = DomainTypeGraphWalker.GetReferencedTypes(type);

            foreach (var referencedType in referencedTypes)
            {
                if (referencedType.Name is not null && ForbiddenTypeNames.Contains(referencedType.Name))
                    violations.Add($"{type.FullName} -> {referencedType.FullName}");
            }
        }

        violations.Should().BeEmpty(
            "domain types must not reference forbidden infrastructure types: " +
            string.Join(", ", violations));
    }

    private static bool IsDomainType(Type type)
    {
        if (type.Namespace is null) return false;
        if (!type.Namespace.StartsWith("Notrelix.Domain.", StringComparison.Ordinal)) return false;
        if (type.Namespace.StartsWith("Notrelix.Domain.SharedKernel", StringComparison.Ordinal)) return false;
        return true;
    }
}
