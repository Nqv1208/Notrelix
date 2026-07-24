using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Ensures all BusinessRuleCodes const string values are unique and non-empty,
/// preventing accidental copy-paste duplicates that would make error diagnostics ambiguous.
/// </summary>
public class RuleCodeUniquenessTests
{
    [Fact]
    public void BusinessRuleCodes_AllValues_ShouldBeUnique()
    {
        var fields = typeof(BusinessRuleCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToList();

        var values = fields.Select(f => (string)f.GetValue(null)!).ToList();

        var duplicates = values
            .GroupBy(v => v)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty(
            "each BusinessRuleCode must have a unique value; duplicates found: " +
            string.Join(", ", duplicates));
    }

    [Fact]
    public void BusinessRuleCodes_AllValues_ShouldBeNonEmpty()
    {
        var fields = typeof(BusinessRuleCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToList();

        var emptyFields = fields
            .Where(f => string.IsNullOrWhiteSpace((string)f.GetValue(null)!))
            .Select(f => f.Name)
            .ToList();

        emptyFields.Should().BeEmpty(
            "each BusinessRuleCode must have a non-empty value; empty fields: " +
            string.Join(", ", emptyFields));
    }

    [Fact]
    public void BusinessRuleCodes_ShouldHaveSubstantialCount()
    {
        var fields = typeof(BusinessRuleCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToList();

        fields.Should().HaveCountGreaterThan(50,
            "sanity check: the Domain should define a substantial number of business rule codes");
    }
}
