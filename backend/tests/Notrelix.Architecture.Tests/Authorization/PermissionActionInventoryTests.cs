using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Architecture.Tests;

/// <summary>
/// WG-TST-ACT-ARCH-001 — WGREQ050–WGREQ053: <see cref="PermissionAction"/> is a
/// stable, business-semantic Governance contract. Every declared action must be
/// either consumed by at least one protected Application request or explicitly
/// documented as a currently-unconsumed contract value; names must be unique and
/// must never encode HTTP verbs.
/// </summary>
public class PermissionActionInventoryTests
{
    /// <summary>
    /// Actions declared in the Governance contract but not yet consumed by any
    /// protected Application request. These are documented, not deleted: WGREQ050
    /// treats additions/removals as traceable contract changes, so an inconsumed
    /// value must be a deliberate, visible inventory entry.
    /// </summary>
    private static readonly string[] DocumentedUnconsumedActions =
    [
        "ArchiveWorkspace",
        "RestoreWorkspace",
        "ManageSpaces",
        "ManageTeams",
        "DeletePage",
    ];

    private static readonly string[] HttpVerbs =
    [
        "Get", "Post", "Put", "Patch", "Delete", "Head", "Options", "Trace",
    ];

    private static string GetApplicationPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.Application");
    }

    private static string[] DeclaredActionNames() =>
        Enum.GetValues<PermissionAction>().Select(a => a.ToString()).ToArray();

    private static HashSet<string> ConsumedActionNames()
    {
        var appPath = GetApplicationPath();
        var files = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        var consumed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("IRequirePermission"))
                continue;

            foreach (Match match in Regex.Matches(content, @"PermissionAction\.(\w+)"))
                consumed.Add(match.Groups[1].Value);
        }

        return consumed;
    }

    [Fact]
    public void PermissionAction_Values_AreUnique()
    {
        var names = DeclaredActionNames();

        names.Should().OnlyHaveUniqueItems(
            "WGREQ051: an action identity is a stable, unique Governance contract value");
    }

    [Fact]
    public void PermissionAction_Names_AreNotHttpVerbs()
    {
        var offenders = DeclaredActionNames()
            .Where(name => HttpVerbs.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        offenders.Should().BeEmpty(
            "WGREQ053: an action is a business capability, never an HTTP verb");
    }

    [Fact]
    public void EveryPermissionAction_IsConsumedOrDocumented()
    {
        var declared = DeclaredActionNames().ToHashSet(StringComparer.Ordinal);
        var consumed = ConsumedActionNames();

        var undocumented = declared
            .Except(consumed)
            .Except(DocumentedUnconsumedActions)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        undocumented.Should().BeEmpty(
            "WGREQ050: every action must be consumed by a protected request or listed in the documented inventory");

        var stale = DocumentedUnconsumedActions
            .Intersect(consumed)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        stale.Should().BeEmpty(
            "a documented unconsumed action must actually remain unconsumed; remove it from the inventory once it is wired up");

        DocumentedUnconsumedActions.Should().OnlyContain(
            name => declared.Contains(name),
            "the documented inventory must only name real PermissionAction values");
    }
}
