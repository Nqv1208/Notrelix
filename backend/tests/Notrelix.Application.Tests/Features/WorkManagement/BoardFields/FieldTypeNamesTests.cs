using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

/// <summary>
/// FIELD-APP-001 (WMREQ027). A field type name parses only to a defined member;
/// unknown names, numeric strings and undefined values fail closed instead of
/// becoming Text.
/// </summary>
public sealed class FieldTypeNamesTests
{
    [Theory]
    [InlineData("Text", FieldType.Text)]
    [InlineData("status", FieldType.Status)]
    public void KnownNames_Parse(string name, FieldType expected)
    {
        FieldTypeNames.TryParse(name, out var type).Should().BeTrue();
        type.Should().Be(expected);
    }

    [Theory]
    [InlineData("Rocket")]
    [InlineData("5")]
    [InlineData("999")]
    [InlineData("")]
    [InlineData(null)]
    public void UnknownOrNumericNames_FailClosed(string? name)
    {
        FieldTypeNames.TryParse(name, out _).Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_RejectsAnUnknownType()
    {
        new CreateBoardFieldCommandValidator()
            .Validate(new CreateBoardFieldCommand(Guid.NewGuid(), "Field", "Rocket", null))
            .IsValid.Should().BeFalse();
    }
}
