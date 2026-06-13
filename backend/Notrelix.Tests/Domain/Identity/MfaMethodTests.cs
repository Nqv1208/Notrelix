using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Mfa.Events;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class MfaMethodTests
{
    private static readonly SecretRef ValidSecret = SecretRef.Create("secret-ref-123");

    [Fact]
    public void Create_ShouldSetPropertiesAndRaiseEvent()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var method = UserMfaMethod.Create(userId, MfaMethodType.AuthenticatorApp, now, ValidSecret);

        method.UserId.Should().Be(userId);
        method.Type.Should().Be(MfaMethodType.AuthenticatorApp);
        method.SecretRef.Should().Be(ValidSecret);
        method.Status.Should().Be(MfaMethodStatus.PendingVerification);
        method.IsVerified.Should().BeFalse();
        method.IsPrimary.Should().BeFalse();
        method.CreatedAt.Should().Be(now);

        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodAddedEvent);
        var evt = (UserMfaMethodAddedEvent)method.DomainEvents.Single(e => e is UserMfaMethodAddedEvent);
        evt.MfaMethodId.Should().Be(method.Id);
        evt.UserId.Should().Be(userId);
        evt.Type.Should().Be(MfaMethodType.AuthenticatorApp);
        evt.AddedAt.Should().Be(now);
    }

    [Fact]
    public void Create_AuthenticatorAppWithoutSecret_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, secretRef: null);

        act.Should().Throw<BusinessRuleException>().WithMessage("*requires a secret reference*");
    }

    [Fact]
    public void Create_EmailSmsWithoutDestination_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;

        var act1 = () => UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.Email, now, destinationMasked: null);
        var act2 = () => UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.Sms, now, destinationMasked: null);

        act1.Should().Throw<BusinessRuleException>().WithMessage("*requires a masked destination*");
        act2.Should().Throw<BusinessRuleException>().WithMessage("*requires a masked destination*");
    }

    [Fact]
    public void Verify_ShouldActivateMethodAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.Email, now, destinationMasked: "t***@example.com");
        method.ClearDomainEvents();

        method.Verify(now.AddMinutes(5));

        method.Status.Should().Be(MfaMethodStatus.Active);
        method.IsVerified.Should().BeTrue();
        method.VerifiedAt.Should().Be(now.AddMinutes(5));

        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodVerifiedEvent);
        var evt = (UserMfaMethodVerifiedEvent)method.DomainEvents.Single(e => e is UserMfaMethodVerifiedEvent);
        evt.MfaMethodId.Should().Be(method.Id);
        evt.UserId.Should().Be(method.UserId);
        evt.Type.Should().Be(MfaMethodType.Email);
        evt.VerifiedAt.Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void Verify_OnDisabledMethod_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);
        method.Disable(now.AddMinutes(1));

        var act = () => method.Verify(now.AddMinutes(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("*disabled MFA method*");
    }

    [Fact]
    public void SetAsPrimary_OnActiveMethod_ShouldSucceedAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);
        method.Verify(now.AddMinutes(1));
        method.ClearDomainEvents();

        method.SetAsPrimary(now.AddMinutes(2));

        method.IsPrimary.Should().BeTrue();
        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodSetAsPrimaryEvent);
        var evt = (UserMfaMethodSetAsPrimaryEvent)method.DomainEvents.Single(e => e is UserMfaMethodSetAsPrimaryEvent);
        evt.MfaMethodId.Should().Be(method.Id);
        evt.UserId.Should().Be(method.UserId);
        evt.Type.Should().Be(method.Type);
        evt.UpdatedAt.Should().Be(now.AddMinutes(2));
    }

    [Fact]
    public void UnsetAsPrimary_ShouldClearPrimaryAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);
        method.Verify(now.AddMinutes(1));
        method.SetAsPrimary(now.AddMinutes(2));
        method.ClearDomainEvents();

        method.UnsetAsPrimary(now.AddMinutes(3));

        method.IsPrimary.Should().BeFalse();
        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodUnsetAsPrimaryEvent);
        var evt = (UserMfaMethodUnsetAsPrimaryEvent)method.DomainEvents.Single(e => e is UserMfaMethodUnsetAsPrimaryEvent);
        evt.MfaMethodId.Should().Be(method.Id);
        evt.UserId.Should().Be(method.UserId);
        evt.Type.Should().Be(method.Type);
        evt.UpdatedAt.Should().Be(now.AddMinutes(3));
    }

    [Fact]
    public void SetAsPrimary_OnPendingMethod_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);

        var act = () => method.SetAsPrimary(now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().WithMessage("*verified and active*");
    }

    [Fact]
    public void Disable_ShouldDeactivateMethodAndClearPrimary()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);
        method.Verify(now.AddMinutes(1));
        method.SetAsPrimary(now.AddMinutes(2));
        method.ClearDomainEvents();

        method.Disable(now.AddMinutes(3));

        method.Status.Should().Be(MfaMethodStatus.Disabled);
        method.IsPrimary.Should().BeFalse();
        method.DisabledAt.Should().Be(now.AddMinutes(3));

        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodDisabledEvent);
        var evt = (UserMfaMethodDisabledEvent)method.DomainEvents.Single(e => e is UserMfaMethodDisabledEvent);
        evt.MfaMethodId.Should().Be(method.Id);
        evt.UserId.Should().Be(method.UserId);
        evt.Type.Should().Be(MfaMethodType.AuthenticatorApp);
        evt.DisabledAt.Should().Be(now.AddMinutes(3));
    }
}
