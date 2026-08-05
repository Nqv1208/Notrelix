namespace Notrelix.Domain.SharedKernel;

/// <summary>
/// Open, extensible resource kind identifier.
/// Format: {context}.{resource}[-{detail}] — lowercase, minimum two dot-separated segments.
/// No global registry — unknown well-formed values are accepted.
/// Equality is ordinal string comparison.
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(ResourceKindJsonConverter))]
public readonly record struct ResourceKind
{
    public string Value { get; }

    private ResourceKind(string value)
    {
        Value = value;
    }

    public static ResourceKind Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > 128)
            throw new ArgumentException("ResourceKind must not exceed 128 characters.", nameof(value));

        var segments = value.Split('.');
        if (segments.Length < 2)
            throw new ArgumentException(
                $"ResourceKind '{value}' must have at least two dot-separated segments (context.resource).",
                nameof(value));

        foreach (var segment in segments)
        {
            if (segment.Length == 0)
                throw new ArgumentException(
                    $"ResourceKind '{value}' contains an empty segment.", nameof(value));

            if (!IsValidSegment(segment))
                throw new ArgumentException(
                    $"ResourceKind '{value}' contains invalid characters. " +
                    "Segments must be lowercase alphanumeric with optional hyphens.",
                    nameof(value));
        }

        return new ResourceKind(value);
    }

    /// <summary>
    /// Attempts to create a ResourceKind without throwing.
    /// Returns false for null, empty, or malformed values.
    /// </summary>
    public static bool TryCreate(string? value, out ResourceKind kind)
    {
        kind = default;

        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
            return false;

        var segments = value.Split('.');
        if (segments.Length < 2)
            return false;

        foreach (var segment in segments)
        {
            if (segment.Length == 0 || !IsValidSegment(segment))
                return false;
        }

        kind = new ResourceKind(value);
        return true;
    }

    private static bool IsValidSegment(string segment)
    {
        foreach (var c in segment)
        {
            if (c is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
                return false;
        }

        return segment[0] is >= 'a' and <= 'z';
    }

    public bool Equals(ResourceKind other) =>
        string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() =>
        Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value ?? "";
}
