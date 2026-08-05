namespace Notrelix.Domain.SharedKernel;

public sealed class Icon : ValueObject
{
    public string Value { get; } = null!;
    public IconType Type { get; }

    private Icon() { }
    private Icon(string value, IconType type)
    {
        Value = value;
        Type = type;
    }

    public static Icon FromEmoji(string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
            throw new ArgumentException("Emoji cannot be empty.", nameof(emoji));

        return new Icon(emoji.Trim(), IconType.Emoji);
    }

    public static Icon FromName(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            throw new ArgumentException("Icon name cannot be empty.", nameof(iconName));

        return new Icon(iconName.Trim().ToLowerInvariant(), IconType.IconName);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Type;
    }

    public override string ToString() => Value;
}
