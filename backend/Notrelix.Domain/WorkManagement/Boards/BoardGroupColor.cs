using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Boards;

public sealed class BoardGroupColor : ValueObject
{
    public string Hex { get; }

    private BoardGroupColor() { }    private BoardGroupColor(string hex)
    {
        Hex = hex;
    }

    public static BoardGroupColor Create(string hex)
    {
        Guard.NotNullOrWhiteSpace(hex);
        return new BoardGroupColor(hex.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hex;
    }
}
