using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.ApiTokens.Abstractions;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.CreateApiToken;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.RevokeApiToken;
using Notrelix.Application.Features.Identity.ApiTokens.Queries.ListApiTokens;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Common.Auditing;
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
    private readonly Mock<IAuditService> _audit = new();

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
        NullLogger<CreateApiTokenCommandHandler>.Instance,
        _audit.Object);

    [Fact]
    public async Task Create_WithValidStepUp_ReturnsRawSecretOnceAndPersistsDigest()
    {
        SetupWorkspaceActor();
        SetupValidStepUp(StepUpPurpose.IssueApiToken);
        var secretService = new FakeApiTokenSecretService();

        var sut = CreateSut(secretService);
        var result = await sut.Handle(new CreateApiTokenCommand(
            TestWorkspaceId, "Deploy token", TestNow.AddDays(30), ValidProof), CancellationToken.None);

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
            TestWorkspaceId, "Deploy token", TestNow.AddDays(30), "forged-token"), CancellationToken.None);

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
            TestWorkspaceId, "Deploy token", TestNow.AddDays(30), ValidProof), CancellationToken.None);

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
            TestWorkspaceId, new string('x', 257), TestNow.AddDays(30), ValidProof), CancellationToken.None);

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
            IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, NullLogger<RevokeApiTokenCommandHandler>.Instance, _audit.Object);
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
            IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, NullLogger<RevokeApiTokenCommandHandler>.Instance, _audit.Object);
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
            IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, NullLogger<RevokeApiTokenCommandHandler>.Instance, _audit.Object);
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
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.First().Id.Should().Be(newer.Id);
        result.Data.Items.Select(t => t.Name).Should().Equal("Newer token", "Older token");
        result.Data.Items.Should().OnlyContain(t =>
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

        result.Data.Items.Should().ContainSingle().Which.Id.Should().Be(own.Id);
    }

    [Fact]
    public async Task Create_WithoutExpiry_IsRefusedBeforeStepUpIsConsumed()
    {
        SetupWorkspaceActor();
        SetupValidStepUp(StepUpPurpose.IssueApiToken);

        var result = await CreateSut().Handle(new CreateApiTokenCommand(
            TestWorkspaceId, "Deploy token", null, ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeFalse("an API token needs an expiry (Lead decision L7)");
        result.Errors.Should().Contain(e => e.Contains("expiration"));
        IdentityContextMock.Object.ApiTokens.Should().BeEmpty();
        StepUpServiceMock.Verify(
            s => s.ConsumeAsync(It.IsAny<string>(), TestUserId, TestSessionId,
                StepUpPurpose.IssueApiToken, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_WithLifetimeAboveTheDefaultMaximum_IsRefused()
    {
        SetupWorkspaceActor();
        SetupValidStepUp(StepUpPurpose.IssueApiToken);

        var result = await CreateSut().Handle(new CreateApiTokenCommand(
            TestWorkspaceId, "Deploy token", TestNow.AddDays(CreateApiTokenCommandHandler.MaxTokenLifetimeDays + 1), ValidProof),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("365 days"));
        IdentityContextMock.Object.ApiTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithLifetimeExactlyAtTheMaximum_IsAccepted()
    {
        SetupWorkspaceActor();
        SetupValidStepUp(StepUpPurpose.IssueApiToken);

        var result = await CreateSut().Handle(new CreateApiTokenCommand(
            TestWorkspaceId, "Deploy token", TestNow.AddDays(CreateApiTokenCommandHandler.MaxTokenLifetimeDays), ValidProof),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    private static bool AuditPayloadIsSecretFree(AuditChange? change, string rawSecret)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(change?.After);
        return change is not null && !payload.Contains(rawSecret) && !payload.Contains("hash:");
    }

    [Fact]
    public async Task Create_RecordsOneAuditRow_WhosePayloadHoldsNoSecret()
    {
        SetupWorkspaceActor();
        SetupValidStepUp(StepUpPurpose.IssueApiToken);
        var secretService = new FakeApiTokenSecretService();

        var result = await CreateSut(secretService).Handle(new CreateApiTokenCommand(
            TestWorkspaceId, "Deploy token", TestNow.AddDays(30), ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _audit.Verify(a => a.RecordAsync(TestWorkspaceId, TestUserId, "CreateApiToken", It.IsAny<ResourceRef>(),
            It.IsAny<AuditMetadata>(), AuditSeverity.Info, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.Is<AuditChange?>(c => AuditPayloadIsSecretFree(c, secretService.LastGeneratedRaw!))), Times.Once);
    }

    [Fact]
    public async Task Revoke_ActiveToken_RecordsOneAuditRow()
    {
        SetupWorkspaceActor();
        var token = CreateToken();
        token.GetType().GetProperty(nameof(ApiToken.Id))!.SetValue(token, Guid.CreateVersion7());
        SetupApiTokens(token);

        var sut = new RevokeApiTokenCommandHandler(
            IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object,
            NullLogger<RevokeApiTokenCommandHandler>.Instance, _audit.Object);
        await sut.Handle(new RevokeApiTokenCommand(TestWorkspaceId, token.Id), CancellationToken.None);

        _audit.Verify(a => a.RecordAsync(TestWorkspaceId, TestUserId, "RevokeApiToken", It.IsAny<ResourceRef>(),
            It.IsAny<AuditMetadata>(), AuditSeverity.Info, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()), Times.Once);
    }

    [Fact]
    public async Task Revoke_AlreadyRevoked_RecordsNoAuditRow()
    {
        SetupWorkspaceActor();
        var token = CreateToken(status: ApiTokenStatus.Revoked);
        SetupApiTokens(token);

        var sut = new RevokeApiTokenCommandHandler(
            IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object,
            NullLogger<RevokeApiTokenCommandHandler>.Instance, _audit.Object);
        await sut.Handle(new RevokeApiTokenCommand(TestWorkspaceId, token.Id), CancellationToken.None);

        _audit.Verify(a => a.RecordAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ResourceRef>(),
            It.IsAny<AuditMetadata>(), It.IsAny<AuditSeverity>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()), Times.Never);
    }

    [Fact]
    public async Task List_PagesNewestFirst_WithADefaultLimitAndAnExactCursor()
    {
        var tokens = Enumerable.Range(0, 51).Select(i =>
        {
            var token = CreateToken(name: $"Token {i}", tokenHash: $"hash:{i}", createdAt: TestNow.AddMinutes(i));
            token.GetType().GetProperty(nameof(ApiToken.Id))!.SetValue(token, Guid.CreateVersion7());
            return token;
        }).ToArray();
        SetupApiTokens(tokens);
        var sut = new ListApiTokensQueryHandler(IdentityContextMock.Object);

        var first = (await sut.Handle(new ListApiTokensQuery(TestWorkspaceId), CancellationToken.None)).Data!;
        first.Items.Should().HaveCount(ListApiTokensQuery.DefaultLimit);
        first.Items.First().Name.Should().Be("Token 50");
        first.NextCursor.Should().NotBeNull();

        var second = (await sut.Handle(new ListApiTokensQuery(TestWorkspaceId, first.NextCursor), CancellationToken.None)).Data!;
        second.Items.Should().ContainSingle().Which.Name.Should().Be("Token 0");
        second.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task List_CapsTheLimitAtTheMaximum_AndRejectsOutOfRangeLimits()
    {
        var tokens = Enumerable.Range(0, 3).Select(i =>
        {
            var token = CreateToken(name: $"Token {i}", tokenHash: $"hash:{i}", createdAt: TestNow.AddMinutes(i));
            token.GetType().GetProperty(nameof(ApiToken.Id))!.SetValue(token, Guid.CreateVersion7());
            return token;
        }).ToArray();
        SetupApiTokens(tokens);

        var page = (await new ListApiTokensQueryHandler(IdentityContextMock.Object)
            .Handle(new ListApiTokensQuery(TestWorkspaceId, null, 2), CancellationToken.None)).Data!;
        page.Items.Should().HaveCount(2);
        page.NextCursor.Should().NotBeNull();

        var validator = new ListApiTokensQueryValidator();
        validator.Validate(new ListApiTokensQuery(TestWorkspaceId, null, ListApiTokensQuery.MaxLimit)).IsValid.Should().BeTrue();
        validator.Validate(new ListApiTokensQuery(TestWorkspaceId, null, ListApiTokensQuery.MaxLimit + 1)).IsValid.Should().BeFalse();
        validator.Validate(new ListApiTokensQuery(TestWorkspaceId, null, 0)).IsValid.Should().BeFalse();
        validator.Validate(new ListApiTokensQuery(TestWorkspaceId, "not-a-cursor", null)).IsValid.Should().BeFalse();
    }
}
