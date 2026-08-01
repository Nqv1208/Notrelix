using FluentAssertions;

namespace Notrelix.Domain.Tests.Common;

public class CommonRuleCodesTests
{
    private static readonly HashSet<string> BoundedContextPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Board",
        "Widget",
        "Invitation",
        "MemberRole",
        "ChildNotFound",
        "Field",
        "Item",
        "View",
        "Form",
        "Template",
        "Page",
        "Block",
        "Comment",
        "Reaction",
        "Mention",
        "Attachment",
        "Automation",
        "Integration",
        "Billing",
        "Subscription",
        "Permission",
        "Role",
        "Policy",
        "Dashboard",
        "Report",
        "Snapshot",
    };

    [Fact]
    public void CommonRuleCodes_ShouldNotContainBoundedContextPrefixes()
    {
        var fields = typeof(CommonRuleCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .ToList();

        var violations = new List<(string Field, string Value)>();

        foreach (var field in fields)
        {
            var value = (string)field.GetValue(null)!;

            foreach (var prefix in BoundedContextPrefixes)
            {
                if (value.Contains(prefix, StringComparison.OrdinalIgnoreCase) &&
                    !value.StartsWith("Common_", StringComparison.Ordinal))
                {
                    violations.Add((field.Name, value));
                    break;
                }
            }
        }

        violations.Should().BeEmpty(
            "CommonRuleCodes must not contain bounded-context vocabulary: " +
            string.Join(", ", violations.Select(v => $"{v.Field}={v.Value}")));
    }

    [Fact]
    public void CommonRuleCodes_ShouldAllStartWithCommonOrGuard()
    {
        var fields = typeof(CommonRuleCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .ToList();

        var violations = fields
            .Where(f =>
            {
                var value = (string)f.GetValue(null)!;
                return !value.StartsWith("Common_", StringComparison.Ordinal) &&
                       !value.StartsWith("Guard_", StringComparison.Ordinal);
            })
            .Select(f => $"{f.Name}={f.GetValue(null)}")
            .ToList();

        violations.Should().BeEmpty(
            "All CommonRuleCodes must start with 'Common_' or 'Guard_': " +
            string.Join(", ", violations));
    }
}
