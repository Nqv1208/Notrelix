using Notrelix.Infrastructure.Auth.Passwords;

namespace Notrelix.Infrastructure.Tests.Auth.Passwords;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashThenVerify_WithSamePassword_ReturnsTrue()
    {
        var hash = _hasher.HashPassword("Password123!");

        hash.Should().NotBeNullOrWhiteSpace();
        _hasher.VerifyPassword("Password123!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.HashPassword("Password123!");

        _hasher.VerifyPassword("DifferentPassword!", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_TwoCalls_ProduceDifferentSaltsButBothVerify()
    {
        var first = _hasher.HashPassword("Password123!");
        var second = _hasher.HashPassword("Password123!");

        first.Should().NotBe(second);
        _hasher.VerifyPassword("Password123!", first).Should().BeTrue();
        _hasher.VerifyPassword("Password123!", second).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithNullOrMalformedHash_ReturnsFalse()
    {
        _hasher.VerifyPassword("Password123!", "").Should().BeFalse();
        _hasher.VerifyPassword("Password123!", "not-a-bcrypt-hash").Should().BeFalse();
        _hasher.VerifyPassword("", "irrelevant").Should().BeFalse();
    }
}
