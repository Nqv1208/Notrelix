using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Common;
using Xunit;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class FrameworkDependencyTests
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

            foreach (var referencedNamespace in GetReferencedNamespaces(type))
            {
                if (ForbiddenNamespaces.Contains(referencedNamespace))
                    violations.Add($"{type.FullName} -> {referencedNamespace}");
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

            foreach (var referencedType in GetReferencedTypes(type))
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

    private static IEnumerable<string> GetReferencedNamespaces(Type type)
    {
        var namespaces = new HashSet<string>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var ns = prop.PropertyType.Namespace;
            if (ns is not null) namespaces.Add(ns);
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var ns = method.ReturnType.Namespace;
            if (ns is not null) namespaces.Add(ns);

            foreach (var param in method.GetParameters())
            {
                ns = param.ParameterType.Namespace;
                if (ns is not null) namespaces.Add(ns);
            }
        }

        foreach (var iface in type.GetInterfaces())
        {
            var ns = iface.Namespace;
            if (ns is not null) namespaces.Add(ns);
        }

        return namespaces;
    }

    private static IEnumerable<Type> GetReferencedTypes(Type type)
    {
        var types = new HashSet<Type>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            types.Add(prop.PropertyType);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            types.Add(method.ReturnType);
            foreach (var param in method.GetParameters())
                types.Add(param.ParameterType);
        }

        return types;
    }
}
