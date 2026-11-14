using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Notrelix.Architecture.Tests.Partitioning;

/// <summary>A read of the Chat messages set whose statement does not name a space id.</summary>
internal sealed record UnscopedMessageRead(string RelativePath, string MemberName, int Line)
{
    /// <summary>The allow-list key: one file and one member (a handler's Handle, a worker's method).</summary>
    public string Key => $"{RelativePath}::{MemberName}";

    public override string ToString() => $"{Key} (line {Line})";
}

/// <summary>
/// Syntax scanner for the partitioning rule (scale plan section 13, item 2): every read of
/// <c>ChatMessages</c> names the space id in the statement that runs it. Writes (Add, Remove, Update, Attach)
/// are exempt: the entity they write carries its own space. Syntax-level only: a statement that mentions a
/// space id counts as filtered, so a rule that never checks the filter's value is out of its reach.
/// </summary>
internal static class ChatMessageSpaceFilterScanner
{
    private const string MessagesSet = "ChatMessages";

    private static readonly HashSet<string> WriteMembers = new(StringComparer.Ordinal)
    {
        "Add", "AddRange", "Remove", "RemoveRange", "Update", "UpdateRange", "Attach", "Entry",
    };

    public static IReadOnlyList<UnscopedMessageRead> ScanFile(string relativePath, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var reads = new List<UnscopedMessageRead>();

        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Name.Identifier.ValueText != MessagesSet)
            {
                continue;
            }

            if (IsWrite(access))
            {
                continue;
            }

            var statement = EnclosingStatement(access);
            if (NamesSpaceId(statement))
            {
                continue;
            }

            var line = tree.GetLineSpan(access.Span).StartLinePosition.Line + 1;
            reads.Add(new UnscopedMessageRead(relativePath, EnclosingMemberName(access), line));
        }

        return reads;
    }

    private static bool IsWrite(MemberAccessExpressionSyntax access) =>
        access.Parent is MemberAccessExpressionSyntax parent
        && parent.Expression == access
        && WriteMembers.Contains(parent.Name.Identifier.ValueText);

    private static SyntaxNode EnclosingStatement(SyntaxNode node) =>
        node.Ancestors().FirstOrDefault(a => a is StatementSyntax or ArrowExpressionClauseSyntax or FieldDeclarationSyntax or PropertyDeclarationSyntax)
        ?? node;

    // "SpaceId", "spaceId", "ChannelSpaceId"; never "WorkspaceId", which is the tenant, not the partition key.
    private static readonly Regex SpaceIdName = new("(?<!work)spaceid$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool NamesSpaceId(SyntaxNode scope) =>
        scope.DescendantNodesAndSelf().OfType<SimpleNameSyntax>()
            .Any(name => SpaceIdName.IsMatch(name.Identifier.ValueText));

    private static string EnclosingMemberName(SyntaxNode node)
    {
        var member = node.Ancestors().FirstOrDefault(a => a is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or FieldDeclarationSyntax or LocalFunctionStatementSyntax);
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax ctor => ctor.Identifier.ValueText,
            LocalFunctionStatementSyntax local => local.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            FieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.ValueText,
            _ => "<top-level>",
        };
    }
}
