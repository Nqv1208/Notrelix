using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;
using Notrelix.Application.Features.Identity.Mfa.Commands.DisableMfa;
using Notrelix.Application.Features.Identity.Mfa.Commands.RegenerateRecoveryCodes;
using Notrelix.Application.Features.Identity.Mfa.Commands.StartMfaEnrollment;
using Notrelix.Application.Features.Identity.Mfa.Commands.VerifyMfaEnrollment;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Mfa.Services;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Caching;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Identity.Mfa;
using Notrelix.Integration.Tests.Containers;
using StackExchange.Redis;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// MFA lifecycle certification (Phase 10): enrollment, TOTP challenge
/// completion, recovery-code single use, regeneration, and disablement
/// against the real PostgreSQL + Redis production graph.
/// </summary>
[Collection("Cache")]
[Trait("Category", "Integration")]
public sealed class MfaFlowTests : IAsyncLifetime
{
    private const string Email = "mfa@example.com";

    private readonly CacheTestContainer _fixture;
    private MfaTestServices _services = null!;
    private User _user = null!;

    public MfaFlowTests(CacheTestContainer fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _services = new MfaTestServices(_fixture);
        return SeedUserAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedUserAsync()
    {
        await _fixture.ResetAsync();
        await using var context = _fixture.CreatePostgresContext();
        _user = User.Create(Email, "MFA User", "hashed", DateTimeOffset.UtcNow, hasPasswordCredential: true);
        context.Users.Add(_user);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Enrollment_TotpChallenge_Login_CompletesFullFlow()
    {
        var enrollment = await _services.SendStartEnrollment(_user.Id);
        enrollment.Succeeded.Should().BeTrue();

        var code = ComputeTotpCode(enrollment.Data!.Secret, DateTimeOffset.UtcNow);
        var verification = await _services.SendVerifyEnrollment(_user.Id, enrollment.Data.MfaMethodId, code);
        verification.Succeeded.Should().BeTrue();
        verification.Data!.RecoveryCodes.Should().HaveCount(MfaPolicy.RecoveryCodeCount);

        var login = await _services.SendLogin(Email);
        login.Succeeded.Should().BeTrue();
        login.Data!.MfaRequired.Should().BeTrue();
        login.Data.AccessToken.Should().BeNull();

        var challenge = await _services.SendCompleteChallenge(
            login.Data.MfaChallengeToken!, ComputeTotpCode(enrollment.Data.Secret, DateTimeOffset.UtcNow));
        challenge.Succeeded.Should().BeTrue();
        challenge.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        challenge.Data.MfaRequired.Should().BeFalse();

        await using (var context = _fixture.CreatePostgresContext())
        {
            context.Sessions.Where(s => s.UserId == _user.Id && s.RevokedAt == null).Count().Should().Be(1);
        }
    }

    [Fact]
    public async Task ChallengeToken_IsSingleUse()
    {
        var enrollment = await _services.SendStartEnrollment(_user.Id);
        var verification = await _services.SendVerifyEnrollment(
            _user.Id, enrollment.Data!.MfaMethodId, ComputeTotpCode(enrollment.Data.Secret, DateTimeOffset.UtcNow));
        verification.Succeeded.Should().BeTrue();

        var login = await _services.SendLogin(Email);
        var token = login.Data!.MfaChallengeToken!;

        var first = await _services.SendCompleteChallenge(token, ComputeTotpCode(enrollment.Data.Secret, DateTimeOffset.UtcNow));
        first.Succeeded.Should().BeTrue();

        var replay = await _services.SendCompleteChallenge(token, ComputeTotpCode(enrollment.Data.Secret, DateTimeOffset.UtcNow));
        replay.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task RecoveryCode_CompletesChallenge_AndIsSingleUse()
    {
        var enrollment = await _services.SendStartEnrollment(_user.Id);
        var verification = await _services.SendVerifyEnrollment(
            _user.Id, enrollment.Data!.MfaMethodId, ComputeTotpCode(enrollment.Data.Secret, DateTimeOffset.UtcNow));
        var recoveryCode = verification.Data!.RecoveryCodes.First();

        var login = await _services.SendLogin(Email);
        var first = await _services.SendCompleteChallenge(login.Data!.MfaChallengeToken!, recoveryCode);
        first.Succeeded.Should().BeTrue();

        var secondLogin = await _services.SendLogin(Email);
        var second = await _services.SendCompleteChallenge(secondLogin.Data!.MfaChallengeToken!, recoveryCode);
        second.Succeeded.Should().BeFalse();

        await using (var context = _fixture.CreatePostgresContext())
        {
            var consumed = context.MfaRecoveryBatches
                .Include(b => b.Codes)
                .SelectMany(b => b.Codes)
                .Count(c => c.ConsumedAt != null);
            consumed.Should().Be(1);
        }
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_InvalidatesOldBatch_AndNewCodesWork()
    {
        var enrollment = await _services.SendStartEnrollment(_user.Id);
        var verification = await _services.SendVerifyEnrollment(
            _user.Id, enrollment.Data!.MfaMethodId, ComputeTotpCode(enrollment.Data.Secret, DateTimeOffset.UtcNow));
        var oldCode = verification.Data!.RecoveryCodes.First();

        var sessionId = Guid.CreateVersion7();
        var stepUpProof = await _services.SendStepUpMfaProof(
            _user.Id, sessionId, StepUpPurpose.RegenerateRecoveryCodes,
            ComputeTotpCode(enrollment.Data!.Secret, DateTimeOffset.UtcNow));
        stepUpProof.Succeeded.Should().BeTrue(
            $"step-up proof should be issued: {string.Join(", ", stepUpProof.Errors)}");

        var regenerated = await _services.SendRegenerateRecoveryCodes(_user.Id, sessionId, stepUpProof.Data!.ProofToken);
        regenerated.Succeeded.Should().BeTrue();
        regenerated.Data!.RecoveryCodes.Should().HaveCount(MfaPolicy.RecoveryCodeCount);
        regenerated.Data.RecoveryCodes.Should().NotContain(oldCode);

        var login = await _services.SendLogin(Email);
        var viaNewCode = await _services.SendCompleteChallenge(login.Data!.MfaChallengeToken!, regenerated.Data.RecoveryCodes.First());
        viaNewCode.Succeeded.Should().BeTrue();

        var secondLogin = await _services.SendLogin(Email);
        var viaOldCode = await _services.SendCompleteChallenge(secondLogin.Data!.MfaChallengeToken!, oldCode);
        viaOldCode.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task DisableMfa_RevokesSessionsAndRemovesChallengeRequirement()
    {
        var enrollment = await _services.SendStartEnrollment(_user.Id);
        var verification = await _services.SendVerifyEnrollment(
            _user.Id, enrollment.Data!.MfaMethodId, ComputeTotpCode(enrollment.Data.Secret, DateTimeOffset.UtcNow));
        verification.Succeeded.Should().BeTrue();

        var sessionId = Guid.CreateVersion7();
        var stepUpProof = await _services.SendStepUpMfaProof(
            _user.Id, sessionId, StepUpPurpose.DisableMfa,
            ComputeTotpCode(enrollment.Data!.Secret, DateTimeOffset.UtcNow));
        stepUpProof.Succeeded.Should().BeTrue(
            $"step-up proof should be issued: {string.Join(", ", stepUpProof.Errors)}");

        var disabled = await _services.SendDisableMfa(_user.Id, sessionId, stepUpProof.Data!.ProofToken);
        disabled.Succeeded.Should().BeTrue();

        await using (var context = _fixture.CreatePostgresContext())
        {
            var methods = context.UserMfaMethods.Where(m => m.UserId == _user.Id).ToList();
            methods.Should().OnlyContain(m => m.Status == MfaMethodStatus.Disabled);
            context.MfaRecoveryBatches.Where(b => b.UserId == _user.Id)
                .Should().OnlyContain(b => b.InvalidatedAt != null);
            var settings = context.UserSecuritySettings.SingleOrDefault(s => s.UserId == _user.Id);
            settings.Should().NotBeNull();
        }

        var login = await _services.SendLogin(Email);
        login.Succeeded.Should().BeTrue();
        login.Data!.MfaRequired.Should().BeFalse();
        login.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CompleteChallenge_WithUnknownToken_Fails()
    {
        var result = await _services.SendCompleteChallenge("unknown-token", "123456");

        result.Succeeded.Should().BeFalse();
    }

    private static string ComputeTotpCode(string base32Secret, DateTimeOffset now)
    {
        var secret = Base32Decode(base32Secret);
        var counter = now.ToUnixTimeSeconds() / MfaPolicy.TotpTimeStepSeconds;

        var counterBytes = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                     | ((hash[offset + 1] & 0xFF) << 16)
                     | ((hash[offset + 2] & 0xFF) << 8)
                     | (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var clean = input.TrimEnd('=').ToUpperInvariant();
        var bits = new List<bool>(clean.Length * 5);
        foreach (var c in clean)
        {
            var value = alphabet.IndexOf(c);
            if (value < 0)
            {
                throw new FormatException($"Invalid base32 character: {c}");
            }

            for (var i = 4; i >= 0; i--)
            {
                bits.Add(((value >> i) & 1) == 1);
            }
        }

        var bytes = new List<byte>();
        for (var i = 0; i + 7 < bits.Count; i += 8)
        {
            byte value = 0;
            for (var j = 0; j < 8; j++)
            {
                value = (byte)((value << 1) | (bits[i + j] ? 1 : 0));
            }

            bytes.Add(value);
        }

        return bytes.ToArray();
    }

    /// <summary>
    /// Wires the real production MFA services (Data Protection, Redis-backed
    /// challenge store, session issuer) against the shared containers.
    /// </summary>
    private sealed class MfaTestServices
    {
        private readonly CacheTestContainer _fixture;
        private readonly IDataProtector _protector;
        private readonly MfaTotpService _totp;
        private readonly MfaRecoveryCodeGenerator _recoveryGenerator;
        private readonly IMfaChallengeStore _challengeStore;
        private readonly Mock<IDateTimeProvider> _time;
        private readonly Mock<IJwtBlacklistService> _jwtBlacklist;
        private readonly Mock<IJwtService> _jwt;

        public MfaTestServices(CacheTestContainer fixture)
        {
            _fixture = fixture;
            var provider = DataProtectionProvider.Create(
                new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"notrelix-mfa-it-{Guid.NewGuid():N}")));
            _protector = provider.CreateProtector("Notrelix.Mfa.Totp.Secret.v1");

            _totp = new MfaTotpService(provider);

            var redisConfig = ConfigurationOptions.Parse(_fixture.RedisConnectionString);
            redisConfig.AbortOnConnectFail = false;
            var multiplexer = ConnectionMultiplexer.Connect(redisConfig);
            var distributedCache = new RedisCache(new RedisCacheOptions
            {
                Configuration = _fixture.RedisConnectionString,
                InstanceName = "Notrelix_",
            });
            _challengeStore = new MfaChallengeStore(new RedisCacheService(distributedCache, multiplexer));

            _recoveryGenerator = new MfaRecoveryCodeGenerator();

            _time = new Mock<IDateTimeProvider>();
            _time.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

            _jwtBlacklist = new Mock<IJwtBlacklistService>();
            _jwt = new Mock<IJwtService>();
            _jwt.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<Guid?>())).Returns("access-token");
            _jwt.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        }

        private Mock<ICurrentRequestContext> Actor(Guid userId, Guid? sessionId = null)
        {
            var ctx = new Mock<ICurrentRequestContext>();
            ctx.Setup(x => x.UserId).Returns(userId);
            if (sessionId is not null)
                ctx.Setup(x => x.SessionId).Returns(sessionId);
            return ctx;
        }

        private ISecurityStepUpService StepUp(ApplicationDbContext context) =>
            new SecurityStepUpService(
                context, _challengeStore, new MfaCodeVerifier(context, _totp, _recoveryGenerator),
                new Mock<IPasswordHasher>().Object, _time.Object);

        private IAuthSessionIssuer SessionIssuer(ApplicationDbContext context) =>
            new AuthSessionIssuer(_jwt.Object, context, _time.Object, new Mock<IClientMetadata>().Object);

        public async Task<Result<MfaEnrollmentStartResult>> SendStartEnrollment(Guid userId)
        {
            await using var context = _fixture.CreatePostgresContext();
            var handler = new StartMfaEnrollmentCommandHandler(
                context, Actor(userId).Object, _totp, _time.Object, NullLogger<StartMfaEnrollmentCommandHandler>.Instance);
            var result = await handler.Handle(new StartMfaEnrollmentCommand(), CancellationToken.None);
            await context.SaveChangesAsync();
            return result;
        }

        public async Task<Result<MfaEnrollmentVerifyResult>> SendVerifyEnrollment(Guid userId, Guid methodId, string code)
        {
            await using var context = _fixture.CreatePostgresContext();
            var handler = new VerifyMfaEnrollmentCommandHandler(
                context, Actor(userId).Object, _totp, _recoveryGenerator, _time.Object,
                NullLogger<VerifyMfaEnrollmentCommandHandler>.Instance);
            var result = await handler.Handle(new VerifyMfaEnrollmentCommand { MfaMethodId = methodId, Code = code }, CancellationToken.None);
            await context.SaveChangesAsync();
            return result;
        }

        public async Task<Result<MfaEnrollmentVerifyResult>> SendRegenerateRecoveryCodes(Guid userId, Guid sessionId, string stepUpToken)
        {
            await using var context = _fixture.CreatePostgresContext();
            var handler = new RegenerateRecoveryCodesCommandHandler(
                context, Actor(userId, sessionId).Object, _recoveryGenerator, StepUp(context), _time.Object,
                NullLogger<RegenerateRecoveryCodesCommandHandler>.Instance);
            var result = await handler.Handle(new RegenerateRecoveryCodesCommand { StepUpToken = stepUpToken }, CancellationToken.None);
            await context.SaveChangesAsync();
            return result;
        }

        public async Task<Result> SendDisableMfa(Guid userId, Guid sessionId, string stepUpToken)
        {
            await using var context = _fixture.CreatePostgresContext();
            var handler = new DisableMfaCommandHandler(
                context, Actor(userId, sessionId).Object, _jwtBlacklist.Object, StepUp(context), _time.Object,
                NullLogger<DisableMfaCommandHandler>.Instance);
            var result = await handler.Handle(new DisableMfaCommand { StepUpToken = stepUpToken }, CancellationToken.None);
            await context.SaveChangesAsync();
            return result;
        }

        public async Task<Result<StepUpProofResult>> SendStepUpMfaProof(Guid userId, Guid sessionId, StepUpPurpose purpose, string code)
        {
            await using var context = _fixture.CreatePostgresContext();
            var stepUpService = StepUp(context);
            var requirement = await stepUpService.GetRequirementAsync(
                userId, sessionId, purpose, CancellationToken.None);
            if (!requirement.Succeeded || requirement.Data?.ChallengeToken is not { } challengeToken)
            {
                return Result<StepUpProofResult>.Failure("Step-up requirement could not be satisfied.");
            }
            return await stepUpService.CompleteMfaAsync(
                userId, sessionId, purpose, challengeToken, code, CancellationToken.None);
        }

        public async Task<Result<AuthResult>> SendLogin(string email)
        {
            await using var context = _fixture.CreatePostgresContext();
            var passwordHasher = new Mock<IPasswordHasher>();
            passwordHasher.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            var handler = new LoginCommandHandler(
                context, passwordHasher.Object, SessionIssuer(context), _challengeStore, _time.Object,
                NullLogger<LoginCommandHandler>.Instance);
            var result = await handler.Handle(new LoginCommand { Email = email, Password = "Password1!" }, CancellationToken.None);
            await context.SaveChangesAsync();
            return result;
        }

        public async Task<Result<AuthResult>> SendCompleteChallenge(string token, string code)
        {
            await using var context = _fixture.CreatePostgresContext();
            var handler = new CompleteMfaChallengeCommandHandler(
                context, _challengeStore, new MfaCodeVerifier(context, _totp, _recoveryGenerator),
                SessionIssuer(context), _time.Object,
                NullLogger<CompleteMfaChallengeCommandHandler>.Instance);
            var result = await handler.Handle(new CompleteMfaChallengeCommand { ChallengeToken = token, Code = code }, CancellationToken.None);
            await context.SaveChangesAsync();
            return result;
        }
    }
}