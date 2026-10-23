using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Identity.ApiTokens.Abstractions;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.CreateApiToken;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.RevokeApiToken;
using Notrelix.Application.Features.Identity.ApiTokens.DTOs;
using Notrelix.Application.Features.Identity.ApiTokens.Queries.ListApiTokens;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendInvitation;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Security.ApiTokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Security follow-up to the workspace role baseline (GOV-013, docs/product/governance.md §44.1),
/// through the real pipeline on PostgreSQL: API-token issuance, listing and revocation are the
/// Owner-only action ManageApiTokens (an Admin gets 403 and nothing is written), and a non-Owner
/// cannot re-arm a legacy Admin invitation by resending it.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceCredentialAndInvitationEscalationTests : IAsyncLifetime
{
    private const string Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
    private static readonly Guid SessionId = Guid.CreateVersion7();

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkspaceCredentialAndInvitationEscalationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IGuestResourceInvitationFacts, GuestResourceInvitationFactsAdapter>();
        services.AddScoped<IApiTokenSecretService, ApiTokenSecretService>();

        var stepUp = new Mock<ISecurityStepUpService>();
        stepUp.Setup(s => s.ConsumeAsync(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        services.AddSingleton(stepUp.Object);

        // The default request context has no session; token issuance needs one.
        var tenantFor = services.BuildServiceProvider();
        var original = tenantFor.GetRequiredService<ICurrentRequestContext>();
        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(original.UserId);
        requestContext.Setup(r => r.SessionId).Returns(SessionId);
        requestContext.Setup(r => r.RequireAccountId()).Returns(() => original.RequireAccountId());
        requestContext.Setup(r => r.RequireWorkspaceId()).Returns(() => original.RequireWorkspaceId());
        services.AddSingleton(requestContext.Object);

        var tokens = new Mock<IOneTimeTokenService>();
        tokens.Setup(t => t.Generate(TokenPurpose.WorkspaceInvitation)).Returns(() =>
            new IssuedOneTimeToken("raw", Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), 1));
        var encryptor = new Mock<ISecretEncryptor>();
        encryptor.Setup(e => e.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("protected");
        services.AddSingleton(tokens.Object);
        services.AddSingleton(encryptor.Object);
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
    }

    private ServiceProvider As(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services => RegisterServices(services));

    private async Task<(OrgStack Stack, Guid Admin)> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        return (stack, admin);
    }

    private async Task<Guid> SeedTokenAsync(OrgStack stack, string name)
    {
        var token = ApiToken.Create(
            stack.AccountId, stack.WorkspaceId, stack.OwnerId, name, Guid.NewGuid().ToString("N"),
            scopes: null, createdBy: stack.OwnerId, createdAt: OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c =>
        {
            c.ApiTokens.Add(token);
            return Task.CompletedTask;
        });
        return token.Id;
    }

    private Task<int> TokenCountAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.ApiTokens.CountAsync(t => t.WorkspaceId == stack.WorkspaceId));

    // ── API tokens: Owner only (GOV-013) ──

    [Fact]
    public async Task Admin_CannotCreateListOrRevokeApiTokens_AndNothingIsWritten()
    {
        var (stack, admin) = await SeedAsync();
        var ownerTokenId = await SeedTokenAsync(stack, "Owner CI token");
        using var asAdmin = As(stack, admin);

        foreach (var request in new object[]
        {
            new CreateApiTokenCommand(stack.WorkspaceId, "Admin minted", null, "step-up"),
            new ListApiTokensQuery(stack.WorkspaceId),
            new RevokeApiTokenCommand(stack.WorkspaceId, ownerTokenId),
        })
        {
            var act = () => OrgTreeTestHost.SendAsync(asAdmin, request);
            await act.Should().ThrowAsync<AppForbidden>(request.GetType().Name);
        }

        (await TokenCountAsync(stack)).Should().Be(1, "a denied Admin issues no credential");
        (await _host.ReadAsync(c => c.ApiTokens.SingleAsync(t => t.Id == ownerTokenId)))
            .Status.Should().Be(ApiTokenStatus.Active, "a denied Admin does not revoke the Owner's token");
    }

    [Fact]
    public async Task Owner_CanCreateListAndRevokeApiTokens()
    {
        var (stack, _) = await SeedAsync();
        using var asOwner = As(stack, stack.OwnerId);

        var created = (Result<CreatedApiTokenDto>)(await OrgTreeTestHost.SendAsync(
            asOwner, new CreateApiTokenCommand(stack.WorkspaceId, "Owner token", null, "step-up")))!;
        created.Data!.RawSecret.Should().NotBeNullOrEmpty();

        var listed = await OrgTreeTestHost.QueryAsync(asOwner, new ListApiTokensQuery(stack.WorkspaceId));
        listed.Should().ContainSingle(t => t.Id == created.Data.Id);

        await OrgTreeTestHost.SendAsync(asOwner, new RevokeApiTokenCommand(stack.WorkspaceId, created.Data.Id));
        (await _host.ReadAsync(c => c.ApiTokens.SingleAsync(t => t.Id == created.Data.Id)))
            .Status.Should().Be(ApiTokenStatus.Revoked);
    }

    // ── ResendInvitation: a non-Owner cannot re-arm a legacy Admin invitation ──

    private async Task<Guid> SeedInvitationAsync(OrgStack stack, WorkspaceRole role)
    {
        var invitation = WorkspaceInvitation.Create(
            stack.AccountId, stack.WorkspaceId, $"{Guid.NewGuid():N}@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(Hash), 1, stack.OwnerId, OrgTreeTestHost.BaseTime);
        // Admin is no longer invitable through Create; a legacy row is simulated by setting the role.
        typeof(WorkspaceInvitation).GetProperty(nameof(WorkspaceInvitation.Role), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(invitation, role);
        await _host.WithSystemContextAsync(c =>
        {
            c.WorkspaceInvitations.Add(invitation);
            return Task.CompletedTask;
        });
        return invitation.Id;
    }

    private Task<WorkspaceInvitation> InvitationAsync(Guid id) =>
        _host.ReadAsync(c => c.WorkspaceInvitations.SingleAsync(i => i.Id == id));

    [Fact]
    public async Task Admin_CannotResendALegacyAdminInvitation_TokenAndGenerationUnchanged()
    {
        var (stack, admin) = await SeedAsync();
        var id = await SeedInvitationAsync(stack, WorkspaceRole.Admin);
        var before = await InvitationAsync(id);
        using var asAdmin = As(stack, admin);

        var act = () => OrgTreeTestHost.SendAsync(asAdmin, new ResendInvitationCommand(stack.WorkspaceId, id));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotAssignRole);
        var after = await InvitationAsync(id);
        after.Token.Value.Should().Be(before.Token.Value);
        after.TokenGeneration.Should().Be(before.TokenGeneration);
    }

    [Fact]
    public async Task Owner_CanResendALegacyAdminInvitation()
    {
        var (stack, _) = await SeedAsync();
        var id = await SeedInvitationAsync(stack, WorkspaceRole.Admin);
        var before = await InvitationAsync(id);
        using var asOwner = As(stack, stack.OwnerId);

        await OrgTreeTestHost.SendAsync(asOwner, new ResendInvitationCommand(stack.WorkspaceId, id));

        var after = await InvitationAsync(id);
        after.Token.Value.Should().NotBe(before.Token.Value);
        after.TokenGeneration.Should().Be(before.TokenGeneration + 1);
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task Admin_CanResendMemberAndGuestInvitations(WorkspaceRole role)
    {
        var (stack, admin) = await SeedAsync();
        var id = await SeedInvitationAsync(stack, role);
        var before = await InvitationAsync(id);
        using var asAdmin = As(stack, admin);

        await OrgTreeTestHost.SendAsync(asAdmin, new ResendInvitationCommand(stack.WorkspaceId, id));

        (await InvitationAsync(id)).TokenGeneration.Should().Be(before.TokenGeneration + 1);
    }
}
