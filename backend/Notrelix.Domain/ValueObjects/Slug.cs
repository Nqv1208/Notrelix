namespace Notrelix.Domain.ValueObjects;

/// <summary>
/// Value object cho URL-safe slug validation
/// </summary>
public class Slug : ValueObject
{
    public string Value { get; private set; }

    private Slug(string value)
    {
        Value = value;
    }

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slug không được để trống", nameof(value));

        var normalized = Normalize(value);

        if (normalized.Length < 2)
            throw new ArgumentException("Slug phải có ít nhất 2 ký tự", nameof(value));

        if (normalized.Length > 100)
            throw new ArgumentException("Slug không được vượt quá 100 ký tự", nameof(value));

        return new Slug(normalized);
    }

    public static Slug GenerateFromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new Slug(Guid.NewGuid().ToString("N")[..8]);

        var slug = Normalize(name);

        if (string.IsNullOrWhiteSpace(slug) || slug.Length < 2)
            slug = Guid.NewGuid().ToString("N")[..8];

        // Append random suffix to avoid collision
        return new Slug($"{slug}-{Guid.NewGuid().ToString("N")[..4]}");
    }

    private static string Normalize(string input)
    {
        return input
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}
