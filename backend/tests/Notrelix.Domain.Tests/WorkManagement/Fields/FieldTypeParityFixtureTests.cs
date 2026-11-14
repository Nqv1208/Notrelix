using System.Text.Json;
using FluentAssertions;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.Tests.WorkManagement.Fields;

/// <summary>
/// The server half of the field-type parity contract (UC-WM-C17, C18). Every value the fixture marks valid is
/// accepted by the Domain validator with the fixture's settings, every invalid one is refused, and every
/// <see cref="FieldType"/> member is in the fixture, so a new type joins it or this test fails.
/// </summary>
public class FieldTypeParityFixtureTests
{
    // Copied next to the test assembly by the project file (the shared file in backend/contracts/fixtures).
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "contracts", "fixtures", "field-type-contract.v1.json");

    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllText(FixturePath));

    private static IEnumerable<(string FieldType, JsonElement Entry)> Entries()
    {
        using var doc = Fixture();
        foreach (var entry in doc.RootElement.GetProperty("types").EnumerateArray().Select(e => e.Clone()))
            yield return (entry.GetProperty("fieldType").GetString()!, entry);
    }

    [Fact]
    public void Fixture_CoversEveryFieldType()
    {
        var covered = Entries().Select(e => e.FieldType).ToHashSet(StringComparer.Ordinal);

        foreach (var type in Enum.GetNames<FieldType>())
            covered.Should().Contain(type, $"the parity fixture must list {type}");
    }

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void Validator_AcceptsEveryValidFixtureValue(string fieldType, string settings, string value)
    {
        var type = Enum.Parse<FieldType>(fieldType);

        Action act = () => FieldValueValidator.Validate(FieldValue.Create(JsonValue.Create(value)), type, FieldSettings.Create(JsonValue.Create(settings)));

        act.Should().NotThrow($"{fieldType} accepts {value}");
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Validator_RefusesEveryInvalidFixtureValue(string fieldType, string settings, string value)
    {
        var type = Enum.Parse<FieldType>(fieldType);

        Action act = () => FieldValueValidator.Validate(FieldValue.Create(JsonValue.Create(value)), type, FieldSettings.Create(JsonValue.Create(settings)));

        act.Should().Throw<BusinessRuleException>($"{fieldType} refuses {value}");
    }

    public static IEnumerable<object[]> ValidCases() => Cases("valid");

    public static IEnumerable<object[]> InvalidCases() => Cases("invalid");

    private static IEnumerable<object[]> Cases(string list)
    {
        foreach (var (fieldType, entry) in Entries())
        {
            var settings = entry.GetProperty("settings").GetRawText();
            foreach (var value in entry.GetProperty(list).EnumerateArray())
                yield return [fieldType, settings, value.GetRawText()];
        }
    }
}
