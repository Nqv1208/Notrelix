using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Mfa.Queries.GetMfaStatus;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Tests.Features.Identity.Mfa;

public class GetMfaStatusQueryTests : IdentityHandlerTestBase
{
    private GetMfaStatusQueryHandler CreateSut() => new(IdentityContextMock.Object, RequestContextMock.Object);

    private UserMfaMethod Method(Guid userId, bool active)
    {
        var method = UserMfaMethod.Create(userId, MfaMethodType.AuthenticatorApp, TestNow, SecretRef.Create("protected"));
        method.GetType().GetProperty(nameof(UserMfaMethod.UserId))!.SetValue(method, userId);
        if (active) method.Verify(TestNow);
        return method;
    }

    private static MfaRecoveryBatch Batch(Guid userId, int count = 8) =>
        MfaRecoveryBatch.Create(userId, Enumerable.Range(0, count).Select(i => $"hash-{userId:N}-{i}"), TestNowStatic, userId);

    private static readonly DateTimeOffset TestNowStatic = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AnActiveFactor_ReportsEnabledWithTheCodesLeft_AndNothingElse()
    {
        SetupUserMfaMethods(Method(TestUserId, active: true));
        var batch = Batch(TestUserId);
        batch.TryConsume($"hash-{TestUserId:N}-0", TestNow, TestUserId);
        SetupMfaRecoveryBatches(batch);

        var result = await CreateSut().Handle(new GetMfaStatusQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.IsEnabled.Should().BeTrue();
        result.Data.PrimaryMethod.Should().Be(MfaMethodType.AuthenticatorApp.ToString());
        result.Data.HasRecoveryCodes.Should().BeTrue();
        result.Data.RecoveryCodesRemaining.Should().Be(7);
        // The body names four facts and no secret, hash or code.
        typeof(MfaStatusDto).GetProperties().Select(p => p.Name).Should().BeEquivalentTo(
            "IsEnabled", "PrimaryMethod", "HasRecoveryCodes", "RecoveryCodesRemaining");
    }

    [Fact]
    public async Task AFactorStillPendingVerification_IsNotEnabled()
    {
        SetupUserMfaMethods(Method(TestUserId, active: false));

        var result = await CreateSut().Handle(new GetMfaStatusQuery(), CancellationToken.None);

        result.Data!.IsEnabled.Should().BeFalse();
        result.Data.PrimaryMethod.Should().BeNull();
        result.Data.RecoveryCodesRemaining.Should().Be(0);
        result.Data.HasRecoveryCodes.Should().BeFalse();
    }

    [Fact]
    public async Task AnInvalidatedBatch_CountsNoCodes()
    {
        SetupUserMfaMethods(Method(TestUserId, active: true));
        var old = Batch(TestUserId);
        old.Invalidate(TestNow, TestUserId);
        SetupMfaRecoveryBatches(old);

        var result = await CreateSut().Handle(new GetMfaStatusQuery(), CancellationToken.None);

        result.Data!.IsEnabled.Should().BeTrue();
        result.Data.HasRecoveryCodes.Should().BeFalse();
        result.Data.RecoveryCodesRemaining.Should().Be(0);
    }

    [Fact]
    public async Task AnotherPersonsFactorAndCodes_NeverShowInMyStatus()
    {
        var theirs = Guid.CreateVersion7();
        SetupUserMfaMethods(Method(theirs, active: true));
        SetupMfaRecoveryBatches(Batch(theirs));

        var result = await CreateSut().Handle(new GetMfaStatusQuery(), CancellationToken.None);

        result.Data!.IsEnabled.Should().BeFalse();
        result.Data.RecoveryCodesRemaining.Should().Be(0);
    }
}
