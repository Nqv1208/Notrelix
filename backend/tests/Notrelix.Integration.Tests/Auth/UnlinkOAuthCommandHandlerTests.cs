using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.UnlinkOAuth;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class UnlinkOAuthCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public UnlinkOAuthCommandHandlerTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static OAuthProfileSnapshot EmptySnapshot(OAuthProvider provider) =>
        OAuthProfileSnapshot.Create(provider, 1, JsonValue.EmptyObject());

    private sealed record StepUpHarness(
        ISecurityStepUpService Service,
        Mock<IPasswordHasher> PasswordHasher,
        Guid SessionId);

    private static StepUpHarness CreateStepUp(ApplicationDbContext context, DateTimeOffset now)
    {
        var clock = FakeDateTimeProvider.WithFixedTime(now);
        var passwordHasher = new Mock<IPasswordHasher>();
        var service = new SecurityStepUpService(
            context,
            new InMemoryMfaChallengeStore(clock),
            new InMemoryStepUpProofStore(clock),
            new Mock<IMfaCodeVerifier>().Object,
            passwordHasher.Object,
            clock);
        return new StepUpHarness(service, passwordHasher, Guid.CreateVersion7());
    }

    private UnlinkOAuthCommandHandler CreateHandler(
        ApplicationDbContext context, Guid currentUserId, StepUpHarness stepUp)
    {
        var currentUser = new Mock<ICurrentRequestContext>();
        currentUser.Setup(x => x.UserId).Returns(currentUserId);
        currentUser.Setup(x => x.SessionId).Returns(stepUp.SessionId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        return new UnlinkOAuthCommandHandler(
            context, currentUser.Object, stepUp.Service, dateTimeProvider.Object,
            NullLogger<UnlinkOAuthCommandHandler>.Instance);
    }

    private static async Task<string> IssuePasswordProof(StepUpHarness stepUp, Guid userId, string password)
    {
        stepUp.PasswordHasher.Setup(x => x.VerifyPassword(password, It.IsAny<string>())).Returns(true);
        var proof = await stepUp.Service.CompletePasswordAsync(
            userId, stepUp.SessionId, StepUpPurpose.UnlinkOAuth, password, CancellationToken.None);
        proof.Succeeded.Should().BeTrue($"password proof failed: {string.Join(", ", proof.Errors)}");
        return proof.Data!.ProofToken;
    }

    private static async Task<string> IssueOAuthProof(StepUpHarness stepUp, Guid userId)
    {
        var proof = await stepUp.Service.GrantOAuthProofAsync(
            userId, stepUp.SessionId, StepUpPurpose.UnlinkOAuth, CancellationToken.None);
        proof.Succeeded.Should().BeTrue();
        return proof.Data!.ProofToken;
    }

    private const string TestPassword = "Password123!";

    [Fact]
    public async Task Handle_WhenPasswordAndLinkedProvider_ShouldUnlink()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("unlink@example.com", "Unlink User", "hashed", now, hasPasswordCredential: true);
        user.LinkOAuthAccount(OAuthProvider.Google, "google-sub-1",
            EmptySnapshot(OAuthProvider.Google), null, user.Id, now);

        Guid userId;
        await using (var seedContext = _db.CreateContext())
        {
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
            userId = user.Id;
        }

        await using var context = _db.CreateContext();
        var stepUp = CreateStepUp(context, now);
        var stepUpToken = await IssuePasswordProof(stepUp, userId, TestPassword);
        var handler = CreateHandler(context, userId, stepUp);

        var result = await handler.Handle(new UnlinkOAuthCommand
        {
            Provider = OAuthProvider.Google,
            StepUpToken = stepUpToken
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();

        var linked = await context.OAuthAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Provider == OAuthProvider.Google);
        linked.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenOAuthOnlySingleProvider_ShouldReturnLastPrimaryAuthMethodConflict()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("oauth-only@example.com", "OAuth Only", "sentinel-hash", now, hasPasswordCredential: false);
        user.LinkOAuthAccount(OAuthProvider.Google, "google-sub-only",
            EmptySnapshot(OAuthProvider.Google), null, user.Id, now);

        Guid userId;
        await using (var seedContext = _db.CreateContext())
        {
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
            userId = user.Id;
        }

        await using var context = _db.CreateContext();
        var stepUp = CreateStepUp(context, now);
        var stepUpToken = await IssueOAuthProof(stepUp, userId);
        var handler = CreateHandler(context, userId, stepUp);

        var result = await handler.Handle(new UnlinkOAuthCommand
        {
            Provider = OAuthProvider.Google,
            StepUpToken = stepUpToken
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().Contain(e => e.Code == "identity.auth.last-primary-auth-method");
        result.TypedErrors.Should().Contain(e => e.Type == ApplicationErrorType.Conflict);

        var linked = await context.OAuthAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Provider == OAuthProvider.Google);
        linked.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenProviderNotLinked_ShouldBeNoOpSuccess()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("no-link@example.com", "No Link", "hashed", now, hasPasswordCredential: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var stepUp = CreateStepUp(context, now);
        var stepUpToken = await IssuePasswordProof(stepUp, user.Id, TestPassword);
        var handler = CreateHandler(context, user.Id, stepUp);

        var result = await handler.Handle(new UnlinkOAuthCommand
        {
            Provider = OAuthProvider.Apple,
            StepUpToken = stepUpToken
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSuspendedUser_ShouldReturnConflict()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("suspended-unlink@example.com", "Suspended", "hashed", now, hasPasswordCredential: true);
        user.LinkOAuthAccount(OAuthProvider.Google, "google-sub-sus",
            EmptySnapshot(OAuthProvider.Google), null, user.Id, now);
        user.Suspend(Guid.NewGuid(), now, "Testing");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var stepUp = CreateStepUp(context, now);
        var stepUpToken = await IssuePasswordProof(stepUp, user.Id, TestPassword);
        var handler = CreateHandler(context, user.Id, stepUp);

        var result = await handler.Handle(new UnlinkOAuthCommand
        {
            Provider = OAuthProvider.Google,
            StepUpToken = stepUpToken
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().Contain(e => e.Code == "identity.auth.account-not-active");

        var linked = await context.OAuthAccounts
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Provider == OAuthProvider.Google);
        linked.Should().NotBeNull();
    }
}
