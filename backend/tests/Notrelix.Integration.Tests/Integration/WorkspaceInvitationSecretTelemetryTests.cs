using Microsoft.AspNetCore.DataProtection;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Security.Encryption;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// WG-SEC-003 / WG-TEST-SEC-MASTER-004 (workspace-governance slice) — secret
/// telemetry. The slice-owned invariant is that the reusable single-use
/// invitation credential is never persisted, never returned to a caller, and
/// never emitted as a raw value in the delivery event; only the purpose-bound
/// hash is stored and only the encrypted <c>ProtectedToken</c> travels. This
/// drives the REAL InviteMember handler over real PostgreSQL with the REAL token
/// issuer, encryptor and event collector, and proves the credential contract.
/// </summary>
[Collection("Database")]
public sealed class WorkspaceInvitationSecretTelemetryTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspaceInvitationSecretTelemetryTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InviteMember_RawInvitationToken_IsNeverPersistedReturnedOrEmittedInCleartext()
    {
        var now = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero);
        var user = User.Create("invite-owner@example.com", "Owner", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);
        var actorId = user.Id;
        var account = Account.Create("Invite Account", $"inv-{Guid.NewGuid():N}", AccountType.Team, actorId, now);
        var accountId = account.Id;
        var slug = $"inv-ws-{Guid.NewGuid():N}";
        var workspace = Workspace.Create(accountId, actorId, "Invite Workspace", slug, now);
        var workspaceId = workspace.Id;

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Users.Add(user);
            seed.Accounts.Add(account);
            seed.Workspaces.Add(workspace);
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(accountId, workspaceId, actorId, WorkspaceRole.Owner, actorId, now));
            await seed.SaveChangesAsync();
        }

        // Real token issuer and real data-protection encryptor, wrapped so the
        // generated raw credential is observable for the leak assertions.
        var issuer = new CapturingOneTimeTokenService(new OneTimeTokenService());
        var encryptor = new SecretEncryptor(new EphemeralDataProtectionProvider());
        var events = new IntegrationEventCollector();

        var tenant = new FakeCurrentTenantContext();
        // WorkspaceInvitation is IWorkspaceScoped, so its global query filter requires
        // the tenant context to match both AccountId and WorkspaceId.
        tenant.SetWorkspace(accountId, workspaceId, actorId);
        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(actorId);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(now);

        await using var context = _db.CreateContext(tenant);
        var handler = new InviteMemberCommandHandler(
            context,
            new ActorLookupService(context),
            requestContext.Object,
            clock.Object,
            issuer,
            encryptor,
            events);

        var command = new InviteMemberCommand(workspaceId, "invitee@example.com", WorkspaceRole.Member);
        var result = await handler.Handle(command, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        result.Succeeded.Should().BeTrue("the authorized owner invitation succeeds");
        var invitationId = result.Data;
        invitationId.Should().NotBe(Guid.Empty, "the result carries the invitation id, not the credential");
        var rawToken = issuer.LastRawToken;
        rawToken.Should().NotBeNullOrWhiteSpace("the issuer produced a reusable credential for this scenario");

        // 1. The raw credential is not persisted: the stored token is the purpose-bound hash.
        var persistedHash = await context.WorkspaceInvitations
            .Where(i => i.Id == invitationId)
            .Select(i => i.Token.Value)
            .SingleAsync();
        persistedHash.Should().NotBe(rawToken, "the raw invitation credential must never be stored");
        persistedHash.Should().NotContain(rawToken, "no persisted value may embed the raw credential");
        persistedHash.Should().HaveLength(InvitationTokenHash.HashLength, "the persisted value is the hash, not the secret");

        // 2. The emitted delivery event carries only the encrypted ProtectedToken,
        //    never the raw credential, and decrypts back to it (so it is protected,
        //    not merely absent).
        var batch = events.CapturePending();
        var delivery = batch.Events.OfType<WorkspaceInvitationDeliveryRequestedIntegrationEventV1>().Single();
        delivery.ProtectedToken.Should().NotBe(rawToken, "the event must not carry the raw credential in cleartext");
        delivery.ProtectedToken.Should().NotBeEmpty();
        encryptor.Unprotect(delivery.ProtectedToken, OneTimeTokenProtectionPurposes.WorkspaceInvitation)
            .Should().Be(rawToken, "the event carries the encrypted form of the credential, proving it is protected rather than dropped");
    }

    [Fact]
    public void DeliveryEvent_ClassifiesProtectedTokenAsSensitive()
    {
        // The reusable credential must be contractually classified so outbox/retry/DLQ
        // diagnostics never log it; the classification lives on the event contract.
        var sensitive = typeof(WorkspaceInvitationDeliveryRequestedIntegrationEventV1)
            .GetCustomAttributes(typeof(EventSensitiveFieldAttribute), inherit: false)
            .Cast<EventSensitiveFieldAttribute>()
            .ToArray();

        sensitive.Should().HaveCount(1, "the delivery event must declare exactly one sensitive field");
        sensitive[0].PropertyName.Should().Be("ProtectedToken",
            "the reusable invitation credential is the sensitive field");
        sensitive[0].Justification.Should().Contain("never be logged",
            "the sensitive-field justification must explicitly forbid logging the credential");
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    /// <summary>
    /// Pass-through decorator over the REAL token issuer that records the raw
    /// credential it produced, so the leak assertions can compare against the
    /// actual secret without changing issuance behavior.
    /// </summary>
    private sealed class CapturingOneTimeTokenService(IOneTimeTokenService inner) : IOneTimeTokenService
    {
        public string? LastRawToken { get; private set; }

        public IssuedOneTimeToken Generate(TokenPurpose purpose)
        {
            var issued = inner.Generate(purpose);
            LastRawToken = issued.RawToken;
            return issued;
        }

        public ParsedOneTimeToken ParseAndHash(string presentedToken, TokenPurpose expectedPurpose)
            => inner.ParseAndHash(presentedToken, expectedPurpose);
    }
}
