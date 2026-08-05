using System.Reflection;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests.DomainPurity;

/// <summary>
/// DOM-BOUND-001..004: Verifies Domain bounded context signature isolation.
/// A Domain type in context X must not expose concrete types from context Y through
/// compiled signatures (base, interfaces, fields, properties, constructors, methods, generics, event payloads).
/// Allowed: same context, Common, SharedKernel, System/BCL.
/// </summary>
public class DomainBoundedContextSignatureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Common.Guard).Assembly;

    private static readonly HashSet<string> AllowedContextPrefixes = new(StringComparer.Ordinal)
    {
        "Notrelix.Domain.Common",
        "Notrelix.Domain.SharedKernel",
    };

    private static readonly HashSet<string> SystemPrefixes = new(StringComparer.Ordinal)
    {
        "System",
        "Microsoft.Extensions",
    };

    private static string? GetContextFromNamespace(string? ns)
    {
        if (ns is null) return null;
        if (!ns.StartsWith("Notrelix.Domain.", StringComparison.Ordinal)) return null;

        var remainder = ns["Notrelix.Domain.".Length..];
        var dotIndex = remainder.IndexOf('.');
        return dotIndex > 0 ? remainder[..dotIndex] : remainder;
    }

    private static bool IsAllowedReference(Type declaringType, Type referencedType)
    {
        var referencedNs = referencedType.Namespace;

        if (referencedNs is null) return true;

        foreach (var prefix in SystemPrefixes)
        {
            if (referencedNs.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        foreach (var prefix in AllowedContextPrefixes)
        {
            if (referencedNs.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        var declaringContext = GetContextFromNamespace(declaringType.Namespace);
        var referencedContext = GetContextFromNamespace(referencedNs);

        if (declaringContext is null || referencedContext is null)
            return true;

        return string.Equals(declaringContext, referencedContext, StringComparison.Ordinal);
    }

    private static IEnumerable<Type> GetDomainTypes()
    {
        return DomainAssembly.GetTypes()
            .Where(t => t is { IsPublic: true, IsInterface: false })
            .Where(t => t.Namespace?.StartsWith("Notrelix.Domain.", StringComparison.Ordinal) == true)
            .Where(t => !t.Namespace!.StartsWith("Notrelix.Domain.Common", StringComparison.Ordinal))
            .Where(t => !t.Namespace!.StartsWith("Notrelix.Domain.SharedKernel", StringComparison.Ordinal));
    }

    [Fact]
    public void DOM_BOUND_001_No_CrossContext_Base_Or_Interface()
    {
        var violations = new List<string>();

        foreach (var type in GetDomainTypes())
        {
            if (type.BaseType is not null && type.BaseType != typeof(object)
                && type.BaseType != typeof(ValueType)
                && !IsAllowedReference(type, type.BaseType))
            {
                violations.Add($"{type.FullName} inherits cross-context base {type.BaseType.FullName}");
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (!IsAllowedReference(type, iface))
                {
                    violations.Add($"{type.FullName} implements cross-context interface {iface.FullName}");
                }
            }
        }

        violations.Should().BeEmpty(
            "Domain types must not expose another bounded context through base class or interface");
    }

    [Fact]
    public void DOM_BOUND_002_No_CrossContext_Property_Field_Constructor_Method()
    {
        var violations = new List<string>();

        foreach (var type in GetDomainTypes())
        {
            var members = new List<(string Kind, Type MemberType)>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                members.Add(("property", prop.PropertyType));

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                members.Add(("field", field.FieldType));

            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var param in ctor.GetParameters())
                    members.Add(("constructor-param", param.ParameterType));
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName) continue;
                members.Add(("method-return", method.ReturnType));
                foreach (var param in method.GetParameters())
                    members.Add(("method-param", param.ParameterType));
            }

            foreach (var (kind, memberType) in members)
            {
                var unwrapped = UnwrapType(memberType);
                foreach (var t in unwrapped)
                {
                    if (!IsAllowedReference(type, t))
                    {
                        violations.Add($"{type.FullName} exposes cross-context {kind}: {t.FullName}");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "Domain types must not expose another bounded context through properties, fields, constructors, or methods");
    }

    [Fact]
    public void DOM_BOUND_003_No_CrossContext_GenericArgument_Or_EventPayload()
    {
        var violations = new List<string>();

        foreach (var type in GetDomainTypes())
        {
            if (type.IsGenericType)
            {
                foreach (var arg in type.GetGenericArguments())
                {
                    foreach (var t in UnwrapType(arg))
                    {
                        if (!IsAllowedReference(type, t))
                            violations.Add($"{type.FullName} has cross-context generic argument: {t.FullName}");
                    }
                }
            }

            if (typeof(DomainEvent).IsAssignableFrom(type))
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (var t in UnwrapType(prop.PropertyType))
                    {
                        if (!IsAllowedReference(type, t))
                            violations.Add($"Domain event {type.FullName} exposes cross-context payload: {t.FullName}");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "Domain events and generic types must not expose another bounded context");
    }

    [Fact]
    public void DOM_BOUND_004_Source_Guard_Detects_Foreign_Context_Using()
    {
        var domainPath = GetDomainPath();
        var violations = new List<string>();

        var contextDirs = Directory.GetDirectories(domainPath)
            .Select(d => Path.GetFileName(d)!)
            .Where(d => d is not ("Common" or "SharedKernel" or "bin" or "obj"))
            .ToList();

        foreach (var context in contextDirs)
        {
            var contextPath = Path.Combine(domainPath, context);
            var csFiles = Directory.GetFiles(contextPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.Combine("bin", "")) && !f.Contains(Path.Combine("obj", "")));

            foreach (var file in csFiles)
            {
                var lines = File.ReadAllLines(file);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("using Notrelix.Domain.", StringComparison.Ordinal))
                        continue;

                    foreach (var otherContext in contextDirs)
                    {
                        if (otherContext == context) continue;

                        if (trimmed.Contains($"Notrelix.Domain.{otherContext}", StringComparison.Ordinal))
                        {
                            var relativePath = Path.GetRelativePath(domainPath, file);
                            violations.Add($"{relativePath}: uses Notrelix.Domain.{otherContext}");
                        }
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "Domain source files must not have explicit using directives to another bounded context");
    }

    private static IEnumerable<Type> UnwrapType(Type type)
    {
        yield return type;

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                foreach (var inner in UnwrapType(arg))
                    yield return inner;
            }
        }

        if (type.IsArray)
        {
            foreach (var inner in UnwrapType(type.GetElementType()!))
                yield return inner;
        }
    }

    private static string GetDomainPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }

        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");

        return Path.Combine(current, "src", "Notrelix.Domain");
    }
}
