using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Accounts;
using Notrelix.Domain.Analytics;
using Notrelix.Domain.Automation;
using Notrelix.Domain.Billing;
using Notrelix.Domain.Collaboration;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Identity;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Ensures all rule code const string values across all per-context classes are unique and non-empty,
/// preventing accidental copy-paste duplicates that would make error diagnostics ambiguous.
/// </summary>
public class RuleCodeUniquenessTests
{
    private static readonly Type[] RuleCodeTypes =
    [
        typeof(CommonRuleCodes),
        typeof(SharedKernelRuleCodes),
        typeof(AccountRuleCodes),
        typeof(IdentityRuleCodes),
        typeof(WorkspaceRuleCodes),
        typeof(WorkManagementRuleCodes),
        typeof(DocumentRuleCodes),
        typeof(CollaborationRuleCodes),
        typeof(AutomationRuleCodes),
        typeof(IntegrationRuleCodes),
        typeof(BillingRuleCodes),
        typeof(GovernanceRuleCodes),
        typeof(AnalyticsRuleCodes),
    ];

    private static List<(string Name, string Value)> GetAllRuleCodes()
    {
        var result = new List<(string, string)>();
        foreach (var type in RuleCodeTypes)
        {
            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            foreach (var field in fields)
                result.Add((field.Name, (string)field.GetValue(null)!));
        }
        return result;
    }

    [Fact]
    public void AllRuleCodes_ShouldBeUnique()
    {
        var codes = GetAllRuleCodes();

        var duplicates = codes
            .GroupBy(c => c.Value)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty(
            "each rule code must have a unique value; duplicates found: " +
            string.Join(", ", duplicates));
    }

    [Fact]
    public void AllRuleCodes_ShouldBeNonEmpty()
    {
        var codes = GetAllRuleCodes();

        var emptyCodes = codes
            .Where(c => string.IsNullOrWhiteSpace(c.Value))
            .Select(c => c.Name)
            .ToList();

        emptyCodes.Should().BeEmpty(
            "each rule code must have a non-empty value; empty fields: " +
            string.Join(", ", emptyCodes));
    }

    [Fact]
    public void AllRuleCodes_ShouldHaveSubstantialCount()
    {
        var codes = GetAllRuleCodes();

        codes.Should().HaveCountGreaterThan(50,
            "sanity check: the Domain should define a substantial number of business rule codes");
    }
}
