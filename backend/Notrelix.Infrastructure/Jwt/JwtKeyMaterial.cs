using System.Security.Cryptography;
using System.Text;

namespace Notrelix.Infrastructure.Jwt;

/// <summary>
/// HS256 cần khóa đối xứng ≥ 256 bit. Chuỗi cấu hình bất kỳ được đưa qua SHA256 → 32 byte cố định.
/// </summary>
public static class JwtKeyMaterial
{
    public static byte[] DeriveKeyBytes(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("JwtSettings:SecretKey cannot be empty.", nameof(secret));

        return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }
}
