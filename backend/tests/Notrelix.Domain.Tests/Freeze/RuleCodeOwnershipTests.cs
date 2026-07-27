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
/// Ensures rule code constants are owned by the correct bounded context class,
/// preventing context-specific codes from leaking into Common or SharedKernel.
/// </summary>
public class RuleCodeOwnershipTests
{
    private static readonly Dictionary<Type, string> RuleCodeOwners = new()
    {
        [typeof(CommonRuleCodes)] = "Common_",
        [typeof(SharedKernelRuleCodes)] = "SharedKernel_",
        [typeof(AccountRuleCodes)] = "Accounts_",
        [typeof(IdentityRuleCodes)] = "Identity_",
        [typeof(WorkspaceRuleCodes)] = "Workspaces_",
        [typeof(WorkManagementRuleCodes)] = "WorkManagement_",
        [typeof(DocumentRuleCodes)] = "Documents_",
        [typeof(CollaborationRuleCodes)] = "Collaboration_",
        [typeof(AutomationRuleCodes)] = "Automation_",
        [typeof(IntegrationRuleCodes)] = "Integrations_",
        [typeof(BillingRuleCodes)] = "Billing_",
        [typeof(GovernanceRuleCodes)] = "Governance_",
        [typeof(AnalyticsRuleCodes)] = "Analytics_",
    };

    private static IEnumerable<(Type Owner, string Name, string Value)> GetAllRuleCodesWithOwner()
    {
        foreach (var (type, _) in RuleCodeOwners)
        {
            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            foreach (var field in fields)
                yield return (type, field.Name, (string)field.GetValue(null)!);
        }
    }

    [Fact]
    public void RuleCodePrefix_ShouldMatchOwnerContext()
    {
        var violations = new List<string>();

        foreach (var (owner, name, value) in GetAllRuleCodesWithOwner())
        {
            var expectedPrefix = RuleCodeOwners[owner];

            // Guard codes are allowed in CommonRuleCodes
            if (owner == typeof(CommonRuleCodes) && value.StartsWith("Guard_"))
                continue;

            if (!value.StartsWith(expectedPrefix))
            {
                violations.Add(
                    $"{owner.Name}.{name} = \"{value}\" should start with \"{expectedPrefix}\"");
            }
        }

        violations.Should().BeEmpty(
            "each rule code value must start with its owner context prefix. Violations:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void CommonRuleCodes_ShouldNotContainContextSpecificVocabulary()
    {
        var contextPrefixes = new[]
        {
            "Accounts_", "Identity_", "Workspaces_", "WorkManagement_",
            "Documents_", "Collaboration_", "Automation_", "Integrations_",
            "Billing_", "Governance_", "Analytics_", "SharedKernel_"
        };

        var commonCodes = typeof(CommonRuleCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        var violations = commonCodes
            .Where(code => contextPrefixes.Any(prefix => code.StartsWith(prefix)))
            .ToList();

        violations.Should().BeEmpty(
            "CommonRuleCodes must not contain context-specific codes. Found:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void SharedKernelRuleCodes_ShouldOnlyContainSharedKernelCodes()
    {
        var codes = typeof(SharedKernelRuleCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (Name: f.Name, Value: (string)f.GetValue(null)!))
            .ToList();

        var violations = codes
            .Where(c => !c.Value.StartsWith("SharedKernel_"))
            .Select(c => $"{c.Name} = \"{c.Value}\"")
            .ToList();

        violations.Should().BeEmpty(
            "SharedKernelRuleCodes must only contain SharedKernel_ prefixed codes. Found:\n" +
            string.Join("\n", violations));
    }
}