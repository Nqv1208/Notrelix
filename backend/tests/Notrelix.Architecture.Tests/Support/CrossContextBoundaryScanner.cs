using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Notrelix.Architecture.Tests;

/// <summary>
/// Cross-context boundary scanner for Application feature code.
///
/// Enforces the backend boundary execution rules:
///   ARCH-BC-001 — foreign persistence dependency (foreign context DbContext abstraction)
///   ARCH-BC-002 — foreign Domain model dependency (producer Domain namespace/type)
///   ARCH-BC-003 — producer internal dependency (producer Abstractions service ports,
///                  producer internal Commands/Queries request namespaces)
///
/// Two detection mechanisms:
///   1. Source scan (Roslyn syntax): explicit per-file using directives and
///      qualified identifier chains, alias-aware, comment/string immune.
///   2. Signature scan (reflection): declaring type surfaces (base, interfaces,
///      fields, properties, constructors, methods, events, attributes, generic
///      constraints) with generic/array/nullable unwrapping.
///
/// Producer `Features.{P}.Public.*` namespaces are the approved cross-context
/// surface and are never flagged. Global usings in GlobalUsings.cs are project
/// level and are not attributed to individual files.
/// </summary>
internal static class CrossContextBoundaryScanner
{
    public const string RuleForeignPersistence = "ARCH-BC-001";
    public const string RuleForeignDomainModel = "ARCH-BC-002";
    public const string RuleProducerInternal = "ARCH-BC-003";

    private const string DomainRoot = "Notrelix.Domain.";
    private const string ApplicationFeaturesRoot = "Notrelix.Application.Features.";
    private const string RelativeFeaturesRoot = "Features.";

    internal static readonly IReadOnlySet<string> BusinessContexts = new HashSet<string>(StringComparer.Ordinal)
    {
        "Accounts",
        "Identity",
        "Workspaces",
        "Governance",
        "WorkManagement",
        "Documents",
        "Collaboration",
        "Automation",
        "Integrations",
        "Billing",
        "Analytics",
    };

    /// <summary>
    /// Context to owned DbContext abstraction mapping. Analytics is intentionally
    /// mapped to IReportingDbContext: the interface is declared under
    /// Features/Analytics/Abstractions and named IReportingDbContext (explicit
    /// ownership mapping, not namespace-derived).
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> ContextDbContextInterface =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Accounts"] = "IAccountDbContext",
            ["Identity"] = "IIdentityDbContext",
            ["Workspaces"] = "IWorkspaceDbContext",
            ["Governance"] = "IGovernanceDbContext",
            ["WorkManagement"] = "IWorkManagementDbContext",
            ["Documents"] = "IDocumentDbContext",
            ["Collaboration"] = "ICollaborationDbContext",
            ["Automation"] = "IAutomationDbContext",
            ["Integrations"] = "IIntegrationDbContext",
            ["Billing"] = "IBillingDbContext",
            ["Analytics"] = "IReportingDbContext",
        };

    internal sealed record SourceReferenceViolation(
        string RuleId,
        string RelativePath,
        int Line,
        string Kind,
        string Chain,
        string ConsumerContext,
        string ProducerContext);

    internal sealed record SignatureReferenceViolation(
        string RuleId,
        string ConsumerType,
        string Surface,
        string ForeignType,
        string ConsumerContext,
        string ProducerContext);

    /// <summary>Resolves the business context from a Features-relative path
    /// (e.g. "Workspaces/Invitations/..."). Returns null for non-consumer paths
    /// (support capabilities such as Search/Operations/Notifications are not
    /// canonical business contexts).</summary>
    internal static string? ResolveContextFromRelativePath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        var marker = "Features/";
        var index = normalized.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
            return null;

        var remainder = normalized[(index + marker.Length)..];
        var slash = remainder.IndexOf('/');
        var candidate = slash > 0 ? remainder[..slash] : remainder;
        return BusinessContexts.Contains(candidate) ? candidate : null;
    }

    // ------------------------------------------------------------------
    // Source scan
    // ------------------------------------------------------------------

    internal static IReadOnlyList<SourceReferenceViolation> ScanSource(
        string source,
        string declaringContext,
        string relativePath)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var root = tree.GetRoot();

        var violations = new List<SourceReferenceViolation>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        void Add(string ruleId, string kind, string chain, SyntaxNode node, string producerContext)
        {
            var key = $"{ruleId}|{kind}|{chain}";
            if (!seen.Add(key))
                return;

            violations.Add(new SourceReferenceViolation(
                ruleId,
                relativePath.Replace('\\', '/'),
                node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                kind,
                chain,
                declaringContext,
                producerContext));
        }

        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case UsingDirectiveSyntax usingDirective:
                    {
                        var name = Normalize(usingDirective.Name.ToFullString());
                        var kind = usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                            ? "using-static"
                            : usingDirective.Alias is not null
                                ? "using-alias"
                                : "using";

                        if (usingDirective.Alias is not null)
                            aliases[usingDirective.Alias.Name.Identifier.Text] = name;

                        if (Classify(name, declaringContext) is { } foreign)
                            Add(foreign.RuleId, kind, foreign.Chain, usingDirective, foreign.ProducerContext);
                        break;
                    }

                case AliasQualifiedNameSyntax aliasQualified
                    when !IsInsideUsingDirective(aliasQualified):
                    {
                        var name = Normalize(aliasQualified.ToString());
                        if (Classify(name, declaringContext) is { } aliasQualifiedForeign)
                            Add(aliasQualifiedForeign.RuleId, "global-qualified", aliasQualifiedForeign.Chain, aliasQualified, aliasQualifiedForeign.ProducerContext);
                        break;
                    }

                case IdentifierNameSyntax identifier
                    when identifier.Identifier.ValueText is "Notrelix" or "Features"
                         && !IsInsideUsingDirective(identifier):
                    {
                        if (IsNamespaceDeclarationName(identifier))
                            break;

                        var chainRoot = GetChainRoot(identifier);
                        var chain = Normalize(chainRoot.ToString());
                        if (Classify(chain, declaringContext) is { } chainForeign)
                        {
                            var kind = chain.StartsWith("global::", StringComparison.Ordinal)
                                ? "global-qualified"
                                : "reference";
                            Add(chainForeign.RuleId, kind, chainForeign.Chain, identifier, chainForeign.ProducerContext);
                        }
                        break;
                    }

                case IdentifierNameSyntax identifier:
                    {
                        if (IsMemberName(identifier))
                            break;

                        if (aliases.TryGetValue(identifier.Identifier.ValueText, out var target)
                            && Classify(target, declaringContext) is { } aliasForeign)
                        {
                            Add(
                                aliasForeign.RuleId,
                                "alias-qualified",
                                $"{identifier.Identifier.ValueText}: {aliasForeign.Chain}",
                                identifier,
                                aliasForeign.ProducerContext);
                        }
                        break;
                    }
            }
        }

        return violations
            .OrderBy(v => v.RelativePath, StringComparer.Ordinal)
            .ThenBy(v => v.Line)
            .ThenBy(v => v.RuleId, StringComparer.Ordinal)
            .ThenBy(v => v.Chain, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Classifies a namespace chain reference. Returns the violated rule and the
    /// producer context, or null when the reference is allowed (own context,
    /// shared kernels, producer Public surface, non-boundary namespaces, or
    /// Abstractions usings which are the signature scanner's responsibility).
    /// </summary>
    private static (string RuleId, string Chain, string ProducerContext)? Classify(
        string chain,
        string declaringContext)
    {
        var normalized = chain.StartsWith("global::", StringComparison.Ordinal)
            ? chain["global::".Length..]
            : chain;

        if (normalized.StartsWith(DomainRoot, StringComparison.Ordinal))
        {
            var segments = normalized.Split('.');
            if (segments.Length < 3)
                return null;

            var candidate = segments[2];
            if (!BusinessContexts.Contains(candidate))
                return null;

            if (string.Equals(candidate, declaringContext, StringComparison.Ordinal))
                return null;

            return (RuleForeignDomainModel, normalized, candidate);
        }

        string? producerContext = null;
        string[] afterContext = [];

        if (normalized.StartsWith(ApplicationFeaturesRoot, StringComparison.Ordinal))
        {
            var segments = normalized.Split('.');
            if (segments.Length < 4)
                return null;

            producerContext = segments[3];
            afterContext = segments.Length > 4 ? segments[4..] : [];
        }
        else if (normalized.StartsWith(RelativeFeaturesRoot, StringComparison.Ordinal))
        {
            var segments = normalized.Split('.');
            if (segments.Length < 3)
                return null;

            producerContext = segments[1];
            afterContext = segments[2..];
        }
        else
        {
            return null;
        }

        if (producerContext is null || !BusinessContexts.Contains(producerContext))
            return null;

        if (string.Equals(producerContext, declaringContext, StringComparison.Ordinal))
            return null;

        if (afterContext.Length > 0 && afterContext[0] == "Public")
            return null;

        if (afterContext.Length > 0 && afterContext[0] == "Abstractions")
            return null;

        var isInternalRequestArea = afterContext.Any(static segment =>
            segment is "Commands" or "Queries");
        if (!isInternalRequestArea)
            return null;

        return (RuleProducerInternal, normalized, producerContext);
    }

    // ------------------------------------------------------------------
    // Signature scan (reflection)
    // ------------------------------------------------------------------

    internal static IReadOnlyList<SignatureReferenceViolation> ScanApplicationFeatureTypes(Assembly applicationAssembly)
    {
        var violations = new List<SignatureReferenceViolation>();

        var consumerTypes = applicationAssembly.GetTypes()
            .Where(IsConsumerType)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        foreach (var type in consumerTypes)
        {
            var declaringContext = ResolveContextFromNamespace(type.Namespace);
            if (declaringContext is null)
                continue;

            ScanTypeSignatures(type, declaringContext, violations);
        }

        return violations
            .OrderBy(v => v.ConsumerType, StringComparer.Ordinal)
            .ThenBy(v => v.RuleId, StringComparer.Ordinal)
            .ThenBy(v => v.Surface, StringComparer.Ordinal)
            .ThenBy(v => v.ForeignType, StringComparer.Ordinal)
            .ToList();
    }

    internal static IReadOnlyList<SignatureReferenceViolation> ScanTypeSignatures(
        Type type,
        string declaringContext)
    {
        var violations = new List<SignatureReferenceViolation>();
        ScanTypeSignatures(type, declaringContext, violations);
        return violations
            .OrderBy(v => v.RuleId, StringComparer.Ordinal)
            .ThenBy(v => v.Surface, StringComparer.Ordinal)
            .ThenBy(v => v.ForeignType, StringComparer.Ordinal)
            .ToList();
    }

    private static void ScanTypeSignatures(
        Type type,
        string declaringContext,
        List<SignatureReferenceViolation> violations)
    {
        void Check(Type? referencedType, string surface)
        {
            if (referencedType is null)
                return;

            foreach (var unwrapped in UnwrapType(referencedType))
            {
                if (IsApprovedPipelineAuthorizationDeclaration(type, unwrapped))
                    continue;

                if (ClassifyReferencedType(unwrapped, declaringContext) is not { } classified)
                    continue;

                violations.Add(new SignatureReferenceViolation(
                    classified.RuleId,
                    type.FullName ?? type.Name,
                    surface,
                    unwrapped.FullName ?? unwrapped.Name,
                    declaringContext,
                    classified.ProducerContext));
            }
        }

        Check(type.BaseType, "base");
        foreach (var iface in type.GetInterfaces())
            Check(iface, "interface");

        foreach (var field in type.GetFields(Flags))
            Check(field.FieldType, $"field:{field.Name}");

        foreach (var property in type.GetProperties(Flags))
        {
            var surface = property.GetIndexParameters().Length > 0
                ? $"property-indexer:{property.Name}"
                : $"property:{property.Name}";
            Check(property.PropertyType, surface);
        }

        foreach (var @event in type.GetEvents(Flags))
            Check(@event.EventHandlerType, $"event:{@event.Name}");

        foreach (var constructor in type.GetConstructors(Flags))
        {
            foreach (var parameter in constructor.GetParameters())
                Check(parameter.ParameterType, $"constructor:{parameter.Name}");
        }

        foreach (var method in type.GetMethods(Flags).Where(m => !m.IsSpecialName))
        {
            Check(method.ReturnType, $"method-return:{method.Name}");
            foreach (var parameter in method.GetParameters())
                Check(parameter.ParameterType, $"method-param:{method.Name}:{parameter.Name}");
        }

        foreach (var genericArgument in type.GetGenericArguments())
        {
            foreach (var constraint in genericArgument.GetGenericParameterConstraints())
                Check(constraint, $"generic-constraint:{genericArgument.Name}");
        }
    }

    /// <summary>
    /// Classifies a referenced CLR type against the declaring context. Returns
    /// the violated rule, or null when allowed (own context, Domain.Common,
    /// Domain.SharedKernel, producer Public surface, non-Notrelix types).
    /// </summary>
    internal static (string RuleId, string ProducerContext)? ClassifyReferencedType(
        Type referencedType,
        string declaringContext)
    {
        if (referencedType.IsGenericParameter)
            return null;

        var ns = referencedType.Namespace;
        if (ns is null)
            return null;

        if (ns.StartsWith(DomainRoot, StringComparison.Ordinal))
        {
            var remainder = ns[DomainRoot.Length..];
            var dot = remainder.IndexOf('.');
            var candidate = dot > 0 ? remainder[..dot] : remainder;
            if (candidate.Length == 0)
                return null;

            if (candidate is "Common" or "SharedKernel")
                return null;

            if (!BusinessContexts.Contains(candidate))
                return null;

            if (string.Equals(candidate, declaringContext, StringComparison.Ordinal))
                return null;

            return (RuleForeignDomainModel, candidate);
        }

        if (!ns.StartsWith(ApplicationFeaturesRoot, StringComparison.Ordinal))
            return null;

        var segments = ns.Split('.');
        if (segments.Length < 4)
            return null;

        var producerContext = segments[3];
        if (!BusinessContexts.Contains(producerContext))
            return null;

        if (string.Equals(producerContext, declaringContext, StringComparison.Ordinal))
            return null;

        if (segments.Length < 5)
            return null;

        var area = segments[4];
        if (area == "Public")
            return null;

        if (area == "Abstractions")
        {
            var name = referencedType.Name;
            if (ContextDbContextInterface.TryGetValue(producerContext, out var expected)
                && name == expected)
                return (RuleForeignPersistence, producerContext);

            return (RuleProducerInternal, producerContext);
        }

        if (area is "Commands" or "Queries")
            return (RuleProducerInternal, producerContext);

        if (segments.Skip(4).Any(static segment => segment is "Commands" or "Queries"))
            return (RuleProducerInternal, producerContext);

        return null;
    }

    /// <summary>
    /// Approved narrow exception: PermissionAction referenced by a request type
    /// implementing the frozen pipeline marker IRequirePermission is the canonical
    /// authorization declaration path (boundary PLAN pipeline-ownership rule /
    /// BOUND-AUTH-001), not a foreign Domain model dependency. Any other reference
    /// to the Governance-owned enum remains a violation.
    /// </summary>
    private static bool IsApprovedPipelineAuthorizationDeclaration(Type declaringType, Type referencedType)
    {
        if (referencedType.FullName != "Notrelix.Domain.Governance.Permissions.PermissionAction")
            return false;

        return declaringType.GetInterfaces()
            .Any(i => i.FullName == "Notrelix.Application.Common.Requests.IRequirePermission");
    }

    private static bool IsInsideUsingDirective(SyntaxNode node)
    {
        return node.Ancestors().OfType<UsingDirectiveSyntax>().Any();
    }

    internal static string? ResolveContextFromNamespace(string? namespaceName)
    {
        if (namespaceName is null || !namespaceName.StartsWith(ApplicationFeaturesRoot, StringComparison.Ordinal))
            return null;

        var remainder = namespaceName[ApplicationFeaturesRoot.Length..];
        var dot = remainder.IndexOf('.');
        var candidate = dot > 0 ? remainder[..dot] : remainder;
        return BusinessContexts.Contains(candidate) ? candidate : null;
    }    private static bool IsConsumerType(Type type)
    {
        if (ResolveContextFromNamespace(type.Namespace) is null)
            return false;

        if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            return false;

        return !type.Name.Contains('<', StringComparison.Ordinal);
    }

    private const BindingFlags Flags =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.DeclaredOnly;

    private static IEnumerable<Type> UnwrapType(Type type)
    {
        var visited = new HashSet<Type>();
        return UnwrapTypeCore(type, visited);
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

        if (type.IsNested && type.DeclaringType is { } declaringType)
        {
            foreach (var inner in UnwrapTypeCore(declaringType, visited))
                yield return inner;
        }
    }

    private static string Normalize(string name)
    {
        return name.Replace("global::", string.Empty, StringComparison.Ordinal).Trim();
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
}
