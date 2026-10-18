namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;

/// <summary>
/// Strict parsing of a public field type name: a defined <see cref="FieldType"/>
/// member name only, case-insensitive. Numeric strings and undefined values,
/// which <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> would accept, are rejected.
/// </summary>
public static class FieldTypeNames
{
    public static bool TryParse(string? value, out FieldType type)
    {
        type = default;
        return !string.IsNullOrWhiteSpace(value)
            && !value.Trim().All(char.IsAsciiDigit)
            && Enum.TryParse(value.Trim(), ignoreCase: true, out type)
            && Enum.IsDefined(type);
    }
}
