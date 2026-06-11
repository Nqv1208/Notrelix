using FluentAssertions;
using Notrelix.Domain.Identity.Security;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class MfaMethodTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var method = UserMfaMethod.Create(userId, MfaMethodType.AuthenticatorApp, now, "secret-ref", "dest-masked", isPrimary: true);

        method.UserId.Should().Be(userId);
        method.Type.Should().Be(MfaMethodType.AuthenticatorApp);
        method.SecretRef.Should().Be("secret-ref");
        method.DestinationMasked.Should().Be("dest-masked");
        method.IsVerified.Should().BeFalse();
        method.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldSetIsVerified()
    {
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.Sms, DateTimeOffset.UtcNow);

        method.Verify(DateTimeOffset.UtcNow);

        method.IsVerified.Should().BeTrue();
        method.VerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetPrimary_ShouldChangeIsPrimary()
    {
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, DateTimeOffset.UtcNow, isPrimary: false);

        method.SetPrimary(true);

        method.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var now = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.Email, now);

        method.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Disable_ShouldSetDisabledAtAndResetPrimary()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, isPrimary: true);

        method.Disable(now.AddDays(1));

        method.DisabledAt.Should().Be(now.AddDays(1));
        method.IsPrimary.Should().BeFalse();
    }
}
