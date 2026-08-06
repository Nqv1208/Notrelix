using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Notrelix.Architecture.Tests.DomainPurity;

internal sealed record DomainReferenceViolation(
    string DeclaringType,
    string Surface,
    string ReferencedType);

/// <summary>
/// FZ-DOM-GATE-01: bounded-context reference graph over compiled signatures and
/// C# source names. See 05-ARCHITECTURE-INFRASTRUCTURE-CONTRACTS.md section 1.
/// </summary>
internal static class DomainReferenceGraph
{
    private const string DomainRoot = "Notrelix.Domain.";
    private const string DomainNamespaceRoot = "Notrelix";
    private const string CommonPrefix = DomainRoot + "Common";
    private const string SharedKernelPrefix = DomainRoot + "SharedKernel";

    private const BindingFlags Flags =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.DeclaredOnly;

    internal static IReadOnlyList<DomainReferenceViolation> Analyze(
        IEnumerable<Type> declaringTypes,
        IReadOnlySet<Type> approvedExternalTypes)
    {
        var violations = new HashSet<DomainReferenceViolation>();

        foreach (var type in declaringTypes)
        {
            var declaringContext = GetContextFromNamespace(type.Namespace);
            if (declaringContext is null) continue;
            if (declaringContext is "Common" or "SharedKernel") continue;

            AnalyzeType(type, declaringContext, approvedExternalTypes, violations);
        }

        return violations
            .OrderBy(v => v.DeclaringType, StringComparer.Ordinal)
            .ThenBy(v => v.Surface, StringComparer.Ordinal)
            .ThenBy(v => v.ReferencedType, StringComparer.Ordinal)
            .ToList();
    }

    internal static IReadOnlyList<string> ScanSource(
        string source,
        string declaringContext,
        IReadOnlyCollection<string> contextNames,
        string displayPath)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var root = tree.GetRoot();

        var violations = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        void Add(string message)
        {
            if (seen.Add(message))
                violations.Add(message);
        }

        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case UsingDirectiveSyntax usingDirective:
                    {
                        var name = NormalizeName(usingDirective.Name.ToFullString());
                        var kind = usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                            ? "using-static"
                            : usingDirective.Alias is not null
                                ? "using-alias"
                                : usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
                                    ? "global-using"
                                    : "using";

                        if (TryGetForeignContext(name, declaringContext, contextNames, out _))
                        {
                            var aliasNote = usingDirective.Alias is not null
                                ? $"{usingDirective.Alias.Name.Identifier.Text} = "
                                : string.Empty;
                            Add($"{displayPath}:{LineOf(usingDirective)}: {kind} {aliasNote}{name}");
                        }

                        if (usingDirective.Alias is not null)
                            aliases[usingDirective.Alias.Name.Identifier.Text] = name;
                        break;
                    }

                case AliasQualifiedNameSyntax aliasQualified:
                    {
                        var name = NormalizeName(aliasQualified.ToString());
                        if (TryGetForeignContext(name, declaringContext, contextNames, out _))
                            Add($"{displayPath}:{LineOf(aliasQualified)}: global-qualified {name}");
                        break;
                    }

                case IdentifierNameSyntax identifier
                    when identifier.Identifier.ValueText == DomainNamespaceRoot:
                    {
                        if (IsNamespaceDeclarationName(identifier))
                            break;

                        var chainRoot = GetChainRoot(identifier);
                        var rawChain = chainRoot.ToString();
                        var chain = NormalizeName(rawChain);
                        if (TryGetForeignContext(chain, declaringContext, contextNames, out _))
                        {
                            var label = rawChain.StartsWith("global::", StringComparison.Ordinal)
                                ? "global-qualified"
                                : "reference";
                            Add($"{displayPath}:{LineOf(identifier)}: {label} {chain}");
                        }
                        break;
                    }

                case IdentifierNameSyntax identifier:
                    {
                        if (IsMemberName(identifier))
                            break;

                        if (aliases.TryGetValue(identifier.Identifier.ValueText, out var target)
                            && TryGetForeignContext(target, declaringContext, contextNames, out _))
                        {
                            Add($"{displayPath}:{LineOf(identifier)}: alias-qualified {identifier.Identifier.ValueText}: {target}");
                        }
                        break;
                    }
            }
        }

        violations.Sort(StringComparer.Ordinal);
        return violations;
    }

    private static void AnalyzeType(
        Type type,
        string declaringContext,
        IReadOnlySet<Type> approvedExternalTypes,
        HashSet<DomainReferenceViolation> violations)
    {
        void Check(Type? referencedType, string surface)
        {
            if (referencedType is null)
                return;

            foreach (var unwrapped in UnwrapType(referencedType))
            {
                if (IsAllowedReference(declaringContext, unwrapped, approvedExternalTypes))
                    continue;

                violations.Add(new DomainReferenceViolation(
                    type.FullName ?? type.Name,
                    surface,
                    unwrapped.FullName ?? unwrapped.Name));
            }
        }

        void CheckAttributes(IEnumerable<CustomAttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                var attributeTypeName = attribute.AttributeType.FullName ?? attribute.AttributeType.Name;

                Check(attribute.AttributeType, $"attribute:{attributeTypeName}");

                foreach (var argument in attribute.ConstructorArguments)
                {
                    foreach (var argumentType in GetAttributeArgumentTypes(argument))
                        Check(argumentType, $"attribute-argument:{attributeTypeName}");
                }

                foreach (var named in attribute.NamedArguments)
                {
                    foreach (var argumentType in GetAttributeArgumentTypes(named.TypedValue))
                        Check(argumentType, $"attribute-argument:{attributeTypeName}");
                }
            }
        }

        void CheckGenericParameters(IEnumerable<Type> genericParameters, string memberName)
        {
            foreach (var genericParameter in genericParameters)
            {
                var surface = $"generic-constraint:{genericParameter.Name}";
                foreach (var constraint in genericParameter.GetGenericParameterConstraints())
                    Check(constraint, surface);

                foreach (var attribute in genericParameter.GetCustomAttributesData())
                {
                    var attributeTypeName = attribute.AttributeType.FullName ?? attribute.AttributeType.Name;
                    Check(attribute.AttributeType, $"generic-parameter-attribute:{genericParameter.Name}:{attributeTypeName}");
                    foreach (var argument in attribute.ConstructorArguments)
                    {
                        foreach (var argumentType in GetAttributeArgumentTypes(argument))
                            Check(argumentType, $"generic-parameter-attribute:{genericParameter.Name}:{attributeTypeName}");
                    }
                }

                _ = memberName;
            }
        }

        Check(type.BaseType, "base");
        foreach (var iface in type.GetInterfaces())
            Check(iface, "interface");

        CheckAttributes(type.GetCustomAttributesData());
        CheckGenericParameters(type.GetGenericArguments(), type.Name);

        foreach (var field in type.GetFields(Flags))
        {
            Check(field.FieldType, $"field:{field.Name}");
            CheckAttributes(field.GetCustomAttributesData());
        }

        foreach (var property in type.GetProperties(Flags))
        {
            var surface = property.GetIndexParameters().Length > 0
                ? $"property-indexer:{property.Name}"
                : $"property:{property.Name}";
            Check(property.PropertyType, surface);
            CheckAttributes(property.GetCustomAttributesData());

            foreach (var indexParameter in property.GetIndexParameters())
            {
                foreach (var attribute in indexParameter.GetCustomAttributesData())
                {
                    var attributeTypeName = attribute.AttributeType.FullName ?? attribute.AttributeType.Name;
                    Check(attribute.AttributeType, $"parameter-attribute:{property.Name}:{indexParameter.Name}:{attributeTypeName}");
                }
            }
        }

        foreach (var @event in type.GetEvents(Flags))
        {
            Check(@event.EventHandlerType, $"event:{@event.Name}");
            CheckAttributes(@event.GetCustomAttributesData());
        }

        foreach (var constructor in type.GetConstructors(Flags))
        {
            var signature = $"({string.Join(", ", constructor.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name))})";
            foreach (var parameter in constructor.GetParameters())
                Check(parameter.ParameterType, $"constructor:{signature}");

            CheckAttributes(constructor.GetCustomAttributesData());
            foreach (var parameter in constructor.GetParameters())
                CheckParameterAttributes(parameter, constructor.Name);
        }

        var methods = type.GetMethods(Flags)
            .Where(m => m.Name is not (".ctor" or ".cctor"))
            .ToList();

        if (type.BaseType == typeof(MulticastDelegate) && methods.All(m => m.Name != "Invoke"))
        {
            var invoke = type.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);
            if (invoke is not null)
                methods.Add(invoke);
        }

        foreach (var method in methods)
        {
            Check(method.ReturnType, $"method-return:{method.Name}");
            foreach (var parameter in method.GetParameters())
                Check(parameter.ParameterType, $"method-param:{method.Name}:{parameter.Name}");

            CheckAttributes(method.GetCustomAttributesData());
            foreach (var attribute in method.ReturnParameter.GetCustomAttributesData())
            {
                var attributeTypeName = attribute.AttributeType.FullName ?? attribute.AttributeType.Name;
                Check(attribute.AttributeType, $"return-attribute:{method.Name}:{attributeTypeName}");
            }

            foreach (var parameter in method.GetParameters())
                CheckParameterAttributes(parameter, method.Name);

            CheckGenericParameters(method.GetGenericArguments(), method.Name);
        }

        void CheckParameterAttributes(ParameterInfo parameter, string memberName)
        {
            foreach (var attribute in parameter.GetCustomAttributesData())
            {
                var attributeTypeName = attribute.AttributeType.FullName ?? attribute.AttributeType.Name;
                Check(attribute.AttributeType, $"parameter-attribute:{memberName}:{parameter.Name}:{attributeTypeName}");
                foreach (var argument in attribute.ConstructorArguments)
                {
                    foreach (var argumentType in GetAttributeArgumentTypes(argument))
                        Check(argumentType, $"parameter-attribute:{memberName}:{parameter.Name}:{attributeTypeName}");
                }
            }
        }
    }

    private static bool IsAllowedReference(
        string declaringContext,
        Type referencedType,
        IReadOnlySet<Type> approvedExternalTypes)
    {
        if (referencedType.IsGenericParameter)
            return true;

        var namespaceName = referencedType.Namespace;
        if (namespaceName is null)
            return true;

        if (namespaceName == "System" || namespaceName.StartsWith("System.", StringComparison.Ordinal))
            return true;

        if (approvedExternalTypes.Contains(referencedType))
            return true;

        if (namespaceName.StartsWith(CommonPrefix, StringComparison.Ordinal))
            return true;

        if (namespaceName.StartsWith(SharedKernelPrefix, StringComparison.Ordinal))
            return true;

        var referencedContext = GetContextFromNamespace(namespaceName);
        if (referencedContext is null)
            return false;

        return string.Equals(declaringContext, referencedContext, StringComparison.Ordinal);
    }

    private static IEnumerable<Type> UnwrapType(Type type)
    {
        var visited = new HashSet<Type>();
        foreach (var unwrapped in UnwrapTypeCore(type, visited))
            yield return unwrapped;
    }

    private static IEnumerable<Type> UnwrapTypeCore(Type type, HashSet<Type> visited)
    {
        if (!visited.Add(type))
            yield break;

        if (type.IsGenericParameter)
        {
            yield return type;
            yield break;
        }

        if (type.IsArray || type.IsByRef || type.IsPointer)
        {
            yield return type;
            if (type.GetElementType() is { } elementType)
            {
                foreach (var inner in UnwrapTypeCore(elementType, visited))
                    yield return inner;
            }

            yield break;
        }

        if (type.IsGenericType)
        {
            yield return type;

            if (!type.IsGenericTypeDefinition)
            {
                foreach (var inner in UnwrapTypeCore(type.GetGenericTypeDefinition(), visited))
                    yield return inner;

                foreach (var argument in type.GetGenericArguments())
                {
                    foreach (var inner in UnwrapTypeCore(argument, visited))
                        yield return inner;
                }
            }

            yield break;
        }

        yield return type;

        if (type.IsEnum)
        {
            foreach (var inner in UnwrapTypeCore(Enum.GetUnderlyingType(type), visited))
                yield return inner;
        }

        if (type.IsNested && type.DeclaringType is { } declaringType)
        {
            foreach (var inner in UnwrapTypeCore(declaringType, visited))
                yield return inner;
        }
    }

    private static IEnumerable<Type> GetAttributeArgumentTypes(CustomAttributeTypedArgument argument)
    {
        if (argument.Value is ReadOnlyCollection<CustomAttributeTypedArgument> nested)
        {
            foreach (var item in nested)
            {
                foreach (var inner in GetAttributeArgumentTypes(item))
                    yield return inner;
            }

            yield break;
        }

        if (argument.Value is Type typeValue)
        {
            foreach (var inner in UnwrapType(typeValue))
                yield return inner;
        }

        var argumentType = argument.ArgumentType;
        if (argumentType.IsArray && argumentType.HasElementType)
        {
            foreach (var inner in UnwrapType(argumentType.GetElementType()!))
                yield return inner;
        }
        else
        {
            foreach (var inner in UnwrapType(argumentType))
                yield return inner;
        }
    }

    private static string? GetContextFromNamespace(string? namespaceName)
    {
        if (namespaceName is null)
            return null;

        if (!namespaceName.StartsWith(DomainRoot, StringComparison.Ordinal))
            return null;

        var remainder = namespaceName[DomainRoot.Length..];
        if (remainder.Length == 0)
            return null;

        var dotIndex = remainder.IndexOf('.');
        return dotIndex > 0 ? remainder[..dotIndex] : remainder;
    }

    private static bool TryGetForeignContext(
        string text,
        string declaringContext,
        IReadOnlyCollection<string> contextNames,
        out string foreignContext)
    {
        foreignContext = string.Empty;

        var normalized = text.StartsWith("global::", StringComparison.Ordinal)
            ? text["global::".Length..]
            : text;

        if (!normalized.StartsWith(DomainRoot, StringComparison.Ordinal))
            return false;

        var segments = normalized.Split('.');
        if (segments.Length < 3)
            return false;

        var candidate = segments[2];
        if (candidate is "Common" or "SharedKernel")
            return false;

        if (!contextNames.Contains(candidate))
            return false;

        if (string.Equals(candidate, declaringContext, StringComparison.Ordinal))
            return false;

        foreignContext = candidate;
        return true;
    }

    private static string NormalizeName(string name)
    {
        return name.Replace("global::", string.Empty, StringComparison.Ordinal);
    }

    private static bool IsNamespaceDeclarationName(IdentifierNameSyntax identifier)
    {
        var current = (SyntaxNode?)identifier;
        while (current?.Parent is QualifiedNameSyntax qualified
               && (qualified.Left == current || qualified.Right == current))
        {
            current = qualified;
        }

        return current?.Parent switch
        {
            NamespaceDeclarationSyntax ns when ns.Name == current => true,
            FileScopedNamespaceDeclarationSyntax fileScoped when fileScoped.Name == current => true,
            _ => false,
        };
    }

    private static SyntaxNode GetChainRoot(SyntaxNode node)
    {
        var current = node;
        while (true)
        {
            switch (current.Parent)
            {
                case QualifiedNameSyntax qualified when qualified.Left == current:
                case MemberAccessExpressionSyntax memberAccess when memberAccess.Expression == current:
                case AliasQualifiedNameSyntax aliasQualified when aliasQualified.Name == current:
                    current = current.Parent!;
                    continue;
                default:
                    return current;
            }
        }
    }

    private static bool IsMemberName(IdentifierNameSyntax identifier)
    {
        return identifier.Parent switch
        {
            QualifiedNameSyntax qualified => qualified.Right == identifier,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name == identifier,
            _ => false,
        };
    }

    private static int LineOf(SyntaxNode node)
    {
        return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    }
}
