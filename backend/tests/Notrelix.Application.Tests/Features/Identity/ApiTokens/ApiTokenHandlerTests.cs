using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.ApiTokens.Abstractions;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.CreateApiToken;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.RevokeApiToken;
using Notrelix.Application.Features.Identity.ApiTokens.Queries.ListApiTokens;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Tests.Features.Identity.ApiTokens;

/// <summary>Deterministic secret service double: digest = "hash:" + raw.</summary>
internal sealed class FakeApiTokenSecretService : IApiTokenSecretService
{
    public string? LastGeneratedRaw { get; private set; }

    public IssuedApiTokenSecret Generate()
    {
        var raw = "ntk_v1.test-secret-token";
        LastGeneratedRaw = raw;
        return new IssuedApiTokenSecret(raw, Hash(raw));
    }

    public string Hash(string rawToken) => "hash:" + rawToken;
}

public class ApiTokenHandlerTests : IdentityHandlerTestBase
{
    private const string ValidProof = "verified-proof-token";
    private readonly Guid TestAccountId = Guid.CreateVersion7();
    private readonly Guid TestWorkspaceId = Guid.CreateVersion7();

    private ApiToken CreateToken(
        Guid? id = null,
        string? name = null,
        Guid? workspaceId = null,
        string? tokenHash = null,
        ApiTokenStatus status = ApiTokenStatus.Active,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? createdAt = null)
    {
        var token = ApiToken.Create(
            TestAccountId,
            workspaceId ?? TestWorkspaceId,
            TestUserId,
            name ?? "Deploy token",
            tokenHash ?? "hash:existing-secret",
            scopes: null,
            createdBy: TestUserId,
            createdAt: createdAt ?? TestNow,
            expiresAt: expiresAt);
        if (id is not null)
        {
            token.GetType().GetProperty(nameof(ApiToken.Id))!.SetValue(token, id.Value);
        }
        if (status == ApiTokenStatus.Revoked)
        {
            token.Revoke(TestUserId, TestNow);
        }
        return token;
    }

    private void SetupWorkspaceActor()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        RequestContextMock.Setup(c => c.RequireAccountId()).Returns(TestAccountId);
        RequestContextMock.Setup(c => c.RequireWorkspaceId()).Returns(TestWorkspaceId);
    }

    private void SetupValidStepUp(StepUpPurpose purpose)
    {
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync(ValidProof, TestUserId, TestSessionId, purpose, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private void SetupFailedStepUp(StepUpPurpose purpose)
    {
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync(It.IsAny<string>(), TestUserId, TestSessionId, purpose, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new ApplicationError(
                "identity.stepup.proof-expired",
                "Step-up proof failed.",
                ApplicationErrorType.PreconditionFailed)));
    }

    private CreateApiTokenCommandHandler CreateSut(FakeApiTokenSecretService? secretService = null) => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        StepUpServiceMock.Object,
        secretService ?? new FakeApiTokenSecretService(),
        DateTimeProviderMock.Object,
        NullLogger<CreateApiTokenCommandHandler>.Instance);

    [Fact]
    public async Task Create_WithValidStepUp_ReturnsRawSecretOnceAndPersistsDigest()
    {
        SetupWorkspaceActor();
        SetupValidStepUp(StepUpPurpose.IssueApiToken);
        var secretService = new FakeApiTokenSecretService();

        var sut = CreateSut(secretService);
        var result = await sut.Handle(new CreateApiTokenCommand(
            TestWorkspaceId, "Deploy token", null, ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.RawSecret.Should().Be(secretService.LastGeneratedRaw);
        result.Data.Name.Should().Be("Deploy token");

        var stored = IdentityContextMock.Object.ApiTokens.Single();
        stored.TokenHash.Should().Be("hash:" + secretService.LastGeneratedRaw,
            "only the digest may be persisted");
        stored.Status.Should().Be(ApiTokenStatus.Active);
        stored.WorkspaceId.Should().Be(TestWorkspaceId);
        stored.AccountId.Should().Be(TestAccountId);
    }

    [Fact]
    public async Task Create_WhenStepUpFails_ReturnsFailureWithoutCreatingToken()
    {
        SetupWorkspaceActor();
        SetupFailedStepUp(StepUpPurpose.IssueApiToken);

        var sut = CreateSut();
        var result = await sut.Handle(new CreateApiTokenCommand(
            TestWorkspaceId, "Deploy token", null, "forged-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        IdentityContextMock.Object.ApiTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WhenSessionMissing_ReturnsFailureWithoutConsumingStepUp()
    {
        SetupWorkspaceActor();
        RequestContextMock.Setup(c => c.SessionId).Returns((Guid?)null);

        var sut = CreateSut();
        var result = await sut.Handle(new CreateApiTokenCommand(
            TestWorkspaceId, "Deploy token", null, ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        IdentityContextMock.Object.ApiTokens.Should().BeEmpty();
        StepUpServiceMock.Verify(
            s => s.ConsumeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_WhenNameTooLong_ReturnsValidationFailureWithoutCreatingToken()
    {
        SetupWorkspaceActor();
        SetupValidStepUp(StepUpPurpose.IssueApiToken);

        var sut = CreateSut();
        var result = await sut.Handle(new CreateApiTokenCommand(
            TestWorkspaceId, new string('x', 257), null, ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Token name"));
        IdentityContextMock.Object.ApiTokens.Should().BeEmpty();
        StepUpServiceMock.Verify(
            s => s.ConsumeAsync(It.IsAny<string>(), TestUserId, TestSessionId,
                StepUpPurpose.IssueApiToken, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_WhenExpiresInPast_ReturnsValidationFailureWithoutCreatingToken()
    {
        SetupWorkspaceActor();
        SetupValidStepUp(StepUpPurpose.IssueApiToken);

        var sut = CreateSut();
        var result = await sut.Handle(new CreateApiTokenCommand(
            TestWorkspaceId, "Deploy token", TestNow.AddMinutes(-1), ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("expiration"));
        IdentityContextMock.Object.ApiTokens.Should().BeEmpty();
        StepUpServiceMock.Verify(
            s => s.ConsumeAsync(It.IsAny<string>(), TestUserId, TestSessionId,
                StepUpPurpose.IssueApiToken, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Revoke_ActiveToken_SetsRevokedState()
    {
        SetupWorkspaceActor();
        var token = CreateToken();
        token.GetType().GetProperty(nameof(ApiToken.Id))!.SetValue(token, Guid.CreateVersion7());
        SetupApiTokens(token);

        var sut = new RevokeApiTokenCommandHandler(
            IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, NullLogger<RevokeApiTokenCommandHandler>.Instance);
        var result = await sut.Handle(new RevokeApiTokenCommand(TestWorkspaceId, token.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        token.Status.Should().Be(ApiTokenStatus.Revoked);
        token.RevokedAt.Should().Be(TestNow);
        token.RevokedBy.Should().Be(TestUserId);
    }

    [Fact]
    public async Task Revoke_AlreadyRevoked_IsSemanticNoOp()
    {
        SetupWorkspaceActor();
        var token = CreateToken(status: ApiTokenStatus.Revoked);
        SetupApiTokens(token);
        var versionBefore = token.Version;

        var sut = new RevokeApiTokenCommandHandler(
            IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, NullLogger<RevokeApiTokenCommandHandler>.Instance);
        var result = await sut.Handle(new RevokeApiTokenCommand(TestWorkspaceId, token.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        token.Status.Should().Be(ApiTokenStatus.Revoked);
        token.Version.Should().Be(versionBefore, "revoking an already-revoked token is a no-op");
    }

    [Fact]
    public async Task Revoke_TokenFromAnotherWorkspace_ThrowsNotFound()
    {
        SetupWorkspaceActor();
        var token = CreateToken(workspaceId: Guid.CreateVersion7());
        SetupApiTokens(token);

        var sut = new RevokeApiTokenCommandHandler(
            IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, NullLogger<RevokeApiTokenCommandHandler>.Instance);
        var act = async () => await sut.Handle(new RevokeApiTokenCommand(TestWorkspaceId, token.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        token.Status.Should().Be(ApiTokenStatus.Active, "foreign-workspace revoke must not mutate state");
    }

    [Fact]
    public async Task List_ReturnsMetadataOnly_OrderedByCreationDescending()
    {
        var older = CreateToken(name: "Older token", tokenHash: "hash:older", createdAt: TestNow.AddMinutes(-30));
        older.GetType().GetProperty(nameof(ApiToken.Id))!.SetValue(older, Guid.CreateVersion7());
        var newer = CreateToken(name: "Newer token", tokenHash: "hash:newer", createdAt: TestNow.AddMinutes(-5));
        newer.GetType().GetProperty(nameof(ApiToken.Id))!.SetValue(newer, Guid.CreateVersion7());
        SetupApiTokens(older, newer);

        var sut = new ListApiTokensQueryHandler(IdentityContextMock.Object);
        var result = await sut.Handle(new ListApiTokensQuery(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.First().Id.Should().Be(newer.Id);
        result.Data.Select(t => t.Name).Should().Equal("Newer token", "Older token");
        result.Data.Should().OnlyContain(t =>
            t.Scopes == null && t.Status == "Active" && t.LastUsedAt == null
            && t.RevokedAt == null && t.ExpiresAt == null);
    }

    [Fact]
    public async Task List_FiltersByWorkspace()
    {
        SetupWorkspaceActor();
        var own = CreateToken();
        own.GetType().GetProperty(nameof(ApiToken.Id))!.SetValue(own, Guid.CreateVersion7());
        var foreign = CreateToken(workspaceId: Guid.CreateVersion7(), name: "Foreign token", tokenHash: "hash:foreign");
        foreign.GetType().GetProperty(nameof(ApiToken.Id))!.SetValue(foreign, Guid.CreateVersion7());
        SetupApiTokens(own, foreign);

        var sut = new ListApiTokensQueryHandler(IdentityContextMock.Object);
        var result = await sut.Handle(new ListApiTokensQuery(TestWorkspaceId), CancellationToken.None);

        result.Data.Should().ContainSingle().Which.Id.Should().Be(own.Id);
    }
}