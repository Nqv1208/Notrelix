using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Reactions;

public sealed class Emoji : ValueObject
{
    public string Code { get; }

    private Emoji() { }    private Emoji(string code)
    {
        Code = code;
    }

    public static Emoji Create(string code)
    {
        Guard.NotNullOrWhiteSpace(code);
        return new Emoji(code.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }
}
