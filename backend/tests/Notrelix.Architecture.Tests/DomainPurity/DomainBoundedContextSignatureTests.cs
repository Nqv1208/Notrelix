using System.Reflection;

namespace Notrelix.Architecture.Tests.DomainPurity;

/// <summary>
/// DOM-BOUND-001..002: verifies bounded-context isolation over the complete
/// compiled signature surface and every production Domain source file.
/// Implemented by <see cref="DomainReferenceGraph"/> (see
/// 05-ARCHITECTURE-INFRASTRUCTURE-CONTRACTS.md section 1).
/// </summary>
public class DomainBoundedContextSignatureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Common.Guard).Assembly;

    [Fact]
    public void DOM_BOUND_001_AllCompiledSignatures_AreContextIsolated()
    {
        var domainTypes = DomainAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Notrelix.Domain.", StringComparison.Ordinal) == true)
            .ToList();

        var violations = DomainReferenceGraph.Analyze(domainTypes, new HashSet<Type>());

        violations.Should().BeEmpty(
            "every Domain signature surface must stay within System, Common, SharedKernel, or its own context:\n" +
            string.Join("\n", violations.Select(v => $"{v.DeclaringType} [{v.Surface}] -> {v.ReferencedType}")));
    }

    [Fact]
    public void DOM_BOUND_002_SourceNames_DoNotReferenceForeignContexts()
    {
        var domainPath = GetDomainPath();

        var contextDirs = Directory.GetDirectories(domainPath)
            .Select(d => Path.GetFileName(d)!)
            .Where(d => d is not ("Common" or "SharedKernel" or "bin" or "obj"))
            .OrderBy(d => d, StringComparer.Ordinal)
            .ToList();

        var violations = new List<string>();

        foreach (var context in contextDirs)
        {
            var contextPath = Path.Combine(domainPath, context);
            var csFiles = Directory.GetFiles(contextPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.Combine("bin", ""), StringComparison.Ordinal)
                            && !f.Contains(Path.Combine("obj", ""), StringComparison.Ordinal));

            foreach (var file in csFiles)
            {
                var source = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(domainPath, file);
                violations.AddRange(DomainReferenceGraph.ScanSource(source, context, contextDirs, relativePath));
            }
        }

        violations.Should().BeEmpty(
            "Domain source must not reference another bounded context:\n" +
            string.Join("\n", violations));
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
