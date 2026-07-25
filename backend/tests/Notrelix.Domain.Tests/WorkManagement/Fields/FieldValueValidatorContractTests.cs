using FluentAssertions;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.Tests.WorkManagement.Fields;

public class FieldValueValidatorContractTests
{
    [Fact]
    public void Number_ShouldAcceptDecimalPrecision()
    {
        var value = FieldValue.Create(JsonValue.Create("123.456"));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Number, settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Number_ShouldEnforceMinMax()
    {
        var settings = FieldSettings.Create(JsonValue.Create("{\"min\":0,\"max\":100}"));
        var tooHigh = FieldValue.Create(JsonValue.Create("150"));

        Action act = () => FieldValueValidator.Validate(tooHigh, FieldType.Number, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*at most*");
    }

    [Fact]
    public void Number_ShouldEnforceMin()
    {
        var settings = FieldSettings.Create(JsonValue.Create("{\"min\":0,\"max\":100}"));
        var tooLow = FieldValue.Create(JsonValue.Create("-10"));

        Action act = () => FieldValueValidator.Validate(tooLow, FieldType.Number, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*at least*");
    }

    [Fact]
    public void Select_ShouldAcceptValidGuid()
    {
        var guid = Guid.NewGuid();
        var value = FieldValue.Create(JsonValue.Create($"\"{guid}\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Select, settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Select_ShouldRejectNonGuidString()
    {
        var value = FieldValue.Create(JsonValue.Create("\"not-a-guid\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Select, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*valid GUID*");
    }

    [Fact]
    public void Select_ShouldRejectEmptyString()
    {
        var value = FieldValue.Create(JsonValue.Create("\"\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Select, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*valid GUID*");
    }

    [Fact]
    public void Status_ShouldAcceptValidGuid()
    {
        var guid = Guid.NewGuid();
        var value = FieldValue.Create(JsonValue.Create($"\"{guid}\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Status, settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Person_ShouldAcceptValidGuid()
    {
        var guid = Guid.NewGuid();
        var value = FieldValue.Create(JsonValue.Create($"\"{guid}\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Person, settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void MultiSelect_ShouldAcceptValidGuidArray()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var value = FieldValue.Create(JsonValue.Create($"[\"{guid1}\",\"{guid2}\"]"));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.MultiSelect, settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void MultiSelect_ShouldRejectNonGuidItems()
    {
        var value = FieldValue.Create(JsonValue.Create("[\"not-a-guid\"]"));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.MultiSelect, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*valid GUIDs*");
    }

    [Fact]
    public void MultiSelect_ShouldRejectDuplicates()
    {
        var guid = Guid.NewGuid();
        var value = FieldValue.Create(JsonValue.Create($"[\"{guid}\",\"{guid}\"]"));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.MultiSelect, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*duplicate*");
    }

    [Fact]
    public void MultiSelect_ShouldRejectEmptyArrayItems()
    {
        var value = FieldValue.Create(JsonValue.Create("[\"\",\"\"]"));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.MultiSelect, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*valid GUIDs*");
    }

    [Fact]
    public void Date_ShouldAcceptIso8601Format()
    {
        var value = FieldValue.Create(JsonValue.Create("\"2024-01-15T10:30:00.000Z\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Date, settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Date_ShouldRejectNonIso8601Format()
    {
        var value = FieldValue.Create(JsonValue.Create("\"not-a-date\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Date, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*valid date*");
    }

    [Fact]
    public void Formula_ShouldRejectWrite()
    {
        var value = FieldValue.Create(JsonValue.Create("\"formula result\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Formula, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*calculated field*");
    }

    [Fact]
    public void Rollup_ShouldRejectWrite()
    {
        var value = FieldValue.Create(JsonValue.Create("42"));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Rollup, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*calculated field*");
    }

    [Fact]
    public void NullValue_ShouldPassValidation()
    {
        var value = FieldValue.Create(JsonValue.Null());
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Text, settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Text_ShouldEnforceMaxLength()
    {
        var settings = FieldSettings.Create(JsonValue.Create("{\"maxLength\":5}"));
        var value = FieldValue.Create(JsonValue.Create("\"exceeds\""));

        Action act = () => FieldValueValidator.Validate(value, FieldType.Text, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*maximum length*");
    }

    [Fact]
    public void Checkbox_ShouldAcceptBoolean()
    {
        var value = FieldValue.Create(JsonValue.Create("true"));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Checkbox, settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Checkbox_ShouldRejectString()
    {
        var value = FieldValue.Create(JsonValue.Create("\"true\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Checkbox, settings);

        act.Should().Throw<BusinessRuleException>().WithMessage("*boolean*");
    }

    [Fact]
    public void Link_ShouldAcceptValidGuid()
    {
        var value = FieldValue.Create(JsonValue.Create("\"https://example.com\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.Link, settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void LongText_ShouldAcceptValidString()
    {
        var value = FieldValue.Create(JsonValue.Create("\"Long text content\""));
        var settings = FieldSettings.Empty();

        Action act = () => FieldValueValidator.Validate(value, FieldType.LongText, settings);

        act.Should().NotThrow();
    }
}
