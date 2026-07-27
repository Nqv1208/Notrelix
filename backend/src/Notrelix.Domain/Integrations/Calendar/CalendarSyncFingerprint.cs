using System.Security.Cryptography;
using System.Text;

namespace Notrelix.Domain.Integrations.Calendar;

public sealed class CalendarSyncFingerprint : ValueObject
{
    public string Value { get; private set; } = null!;

    private CalendarSyncFingerprint() { }
    private CalendarSyncFingerprint(string value)
    {
        Value = value;
    }

    public static CalendarSyncFingerprint Create(string? title, DateTime? dueDate)
    {
        var input = $"{title ?? ""}|{dueDate?.ToString("O") ?? ""}";
        var hash = ComputeSha256(input);
        return new CalendarSyncFingerprint(hash);
    }

    public bool HasChanged(string? title, DateTime? dueDate)
    {
        var newHash = Create(title, dueDate);
        return Value != newHash.Value;
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(CalendarSyncFingerprint hash) => hash.Value;
}
