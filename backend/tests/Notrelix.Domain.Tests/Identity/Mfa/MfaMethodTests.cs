using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

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

        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodAddedDomainEvent);
        var evt = (UserMfaMethodAddedDomainEvent)method.DomainEvents.Single(e => e is UserMfaMethodAddedDomainEvent);
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
        ((IHasDomainEvents)method).ClearDomainEvents();

        method.Verify(now.AddMinutes(5));

        method.Status.Should().Be(MfaMethodStatus.Active);
        method.IsVerified.Should().BeTrue();
        method.VerifiedAt.Should().Be(now.AddMinutes(5));

        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodVerifiedDomainEvent);
        var evt = (UserMfaMethodVerifiedDomainEvent)method.DomainEvents.Single(e => e is UserMfaMethodVerifiedDomainEvent);
        evt.MfaMethodId.Should().Be(method.Id);
        evt.UserId.Should().Be(method.UserId);
        evt.Type.Should().Be(MfaMethodType.Email);
        evt.VerifiedAt.Should().Be(now.AddMinutes(5));
    }

    [CoversMutation(typeof(UserMfaMethod), nameof(UserMfaMethod.Verify), MutationScenario.Invalid, typeof(DateTimeOffset))]
    [Fact]
    public void Verify_OnDisabledMethod_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);
        method.Disable(now.AddMinutes(1));

        var act = () => method.Verify(now.AddMinutes(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("*disabled MFA method*");
    }

    [CoversMutation(typeof(UserMfaMethod), nameof(UserMfaMethod.SetAsPrimary), MutationScenario.Event, typeof(DateTimeOffset))]
    [Fact]
    public void SetAsPrimary_OnActiveMethod_ShouldSucceedAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);
        method.Verify(now.AddMinutes(1));
        ((IHasDomainEvents)method).ClearDomainEvents();

        method.SetAsPrimary(now.AddMinutes(2));

        method.IsPrimary.Should().BeTrue();
        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodSetAsPrimaryDomainEvent);
        var evt = (UserMfaMethodSetAsPrimaryDomainEvent)method.DomainEvents.Single(e => e is UserMfaMethodSetAsPrimaryDomainEvent);
        evt.MfaMethodId.Should().Be(method.Id);
        evt.UserId.Should().Be(method.UserId);
        evt.Type.Should().Be(method.Type);
        evt.UpdatedAt.Should().Be(now.AddMinutes(2));
    }

    [CoversMutation(typeof(UserMfaMethod), nameof(UserMfaMethod.UnsetAsPrimary), MutationScenario.Event, typeof(DateTimeOffset))]
    [Fact]
    public void UnsetAsPrimary_ShouldClearPrimaryAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);
        method.Verify(now.AddMinutes(1));
        method.SetAsPrimary(now.AddMinutes(2));
        ((IHasDomainEvents)method).ClearDomainEvents();

        method.UnsetAsPrimary(now.AddMinutes(3));

        method.IsPrimary.Should().BeFalse();
        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodUnsetAsPrimaryDomainEvent);
        var evt = (UserMfaMethodUnsetAsPrimaryDomainEvent)method.DomainEvents.Single(e => e is UserMfaMethodUnsetAsPrimaryDomainEvent);
        evt.MfaMethodId.Should().Be(method.Id);
        evt.UserId.Should().Be(method.UserId);
        evt.Type.Should().Be(method.Type);
        evt.UpdatedAt.Should().Be(now.AddMinutes(3));
    }

    [CoversMutation(typeof(UserMfaMethod), nameof(UserMfaMethod.SetAsPrimary), MutationScenario.Invalid, typeof(DateTimeOffset))]
    [Fact]
    public void SetAsPrimary_OnPendingMethod_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);

        var act = () => method.SetAsPrimary(now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().WithMessage("*verified and active*");
    }

    [CoversMutation(typeof(UserMfaMethod), nameof(UserMfaMethod.Disable), MutationScenario.Event, typeof(DateTimeOffset))]
    [Fact]
    public void Disable_ShouldDeactivateMethodAndClearPrimary()
    {
        var now = DateTimeOffset.UtcNow;
        var method = UserMfaMethod.Create(Guid.NewGuid(), MfaMethodType.AuthenticatorApp, now, ValidSecret);
        method.Verify(now.AddMinutes(1));
        method.SetAsPrimary(now.AddMinutes(2));
        ((IHasDomainEvents)method).ClearDomainEvents();

        method.Disable(now.AddMinutes(3));

        method.Status.Should().Be(MfaMethodStatus.Disabled);
        method.IsPrimary.Should().BeFalse();
        method.DisabledAt.Should().Be(now.AddMinutes(3));

        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodDisabledDomainEvent);
        var evt = (UserMfaMethodDisabledDomainEvent)method.DomainEvents.Single(e => e is UserMfaMethodDisabledDomainEvent);
        evt.MfaMethodId.Should().Be(method.Id);
        evt.UserId.Should().Be(method.UserId);
        evt.Type.Should().Be(MfaMethodType.AuthenticatorApp);
        evt.DisabledAt.Should().Be(now.AddMinutes(3));
    }
}
