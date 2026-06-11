using System.Security.Cryptography;
using System.Text;

namespace Notrelix.Domain.SharedKernel;

/// <summary>
/// Value object cho sync hash — phát hiện thay đổi giữa app và external calendar
/// Hash(title + dueDate) → tránh vòng lặp sync vô hạn
/// </summary>
public class SyncHash : ValueObject
{
    public string Value { get; private set; }

    private SyncHash(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Tạo hash từ title và due date
    /// </summary>
    public static SyncHash Create(string? title, DateTime? dueDate)
    {
        var input = $"{title ?? ""}|{dueDate?.ToString("O") ?? ""}";
        var hash = ComputeSha256(input);
        return new SyncHash(hash);
    }

    /// <summary>
    /// So sánh hash hiện tại với dữ liệu mới — nếu khác thì cần sync
    /// </summary>
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

    public static implicit operator string(SyncHash hash) => hash.Value;
}
