using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Public.Access;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Users.Services;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ResetMemberMfa;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The Owner's reset of a member's second factor through the real pipeline on PostgreSQL with the real policy
/// engine and the real step-up service: only the Owner, only with a fresh proof bound to this member, never for
/// an Owner. A refusal leaves the factor, the recovery codes and the sessions as they were.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ResetMemberMfaIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;
    private readonly HandoverStepUp _stepUp;
    private readonly Mock<ISecurityActivityRecorder> _activity = new();
    private readonly Dictionary<Guid, Guid> _sessions = new();

    public ResetMemberMfaIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(() => _host.Now);
        _stepUp = new HandoverStepUp(clock.Object);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(_stepUp.Service);
        services.AddSingleton(_activity.Object);
        services.AddSingleton(new Mock<ISecurityNotice>().Object);
        services.AddSingleton(new Mock<IJwtBlacklistService>().Object);
        services.AddScoped<IIdentityUserAccess, IdentityUserAccessProvider>();
    }

    private Guid SessionOf(Guid userId) => _sessions.TryGetValue(userId, out var id) ? id : _sessions[userId] = Guid.CreateVersion7();

    private ServiceProvider As(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, RegisterServices, SessionOf(userId));

    private async Task<string> ProofAsync(Guid caller, Guid workspaceId, Guid member)
    {
        var granted = await _stepUp.Service.GrantOAuthProofAsync(
            caller, SessionOf(caller), StepUpPurpose.ResetMemberMfa,
            StepUpTarget.MemberMfaReset(workspaceId, member), CancellationToken.None);
        granted.Succeeded.Should().BeTrue();
        return granted.Data!.ProofToken;
    }

    private static async Task<Result> TrySendAsync(ServiceProvider provider, ResetMemberMfaCommand command)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    /// <summary>A member with an active authenticator, one recovery batch and one live session.</summary>
    private async Task<Guid> SeedEnrolledMemberAsync(OrgStack stack, WorkspaceRole role)
    {
        var member = await _host.AddMemberAsync(stack, role, "Enrolled");
        await _host.WithSystemContextAsync(c =>
        {
            var method = UserMfaMethod.Create(member, MfaMethodType.AuthenticatorApp, OrgTreeTestHost.BaseTime, SecretRef.Create("s"));
            method.Verify(OrgTreeTestHost.BaseTime);
            c.UserMfaMethods.Add(method);
            c.MfaRecoveryBatches.Add(MfaRecoveryBatch.Create(member, ["h1", "h2"], OrgTreeTestHost.BaseTime, member));
            c.Sessions.Add(UserSession.Create(
                member, RefreshTokenHash.Create($"raw-{Guid.NewGuid()}"), OrgTreeTestHost.BaseTime.AddDays(30), OrgTreeTestHost.BaseTime));
            return Task.CompletedTask;
        });
        return member;
    }

    private async Task AssertUntouchedAsync(Guid member)
    {
        (await _host.ReadAsync(c => c.UserMfaMethods.CountAsync(m => m.UserId == member && m.Status == MfaMethodStatus.Active))).Should().Be(1);
        (await _host.ReadAsync(c => c.MfaRecoveryBatches.CountAsync(b => b.UserId == member && b.InvalidatedAt == null))).Should().Be(1);
        (await _host.ReadAsync(c => c.Sessions.CountAsync(s => s.UserId == member && s.Status == SessionStatus.Active))).Should().Be(1);
    }

    [Fact]
    public async Task TheOwner_ResetsAMembersFactor_WhichEndsTheirCodesAndSessions_AndRecordsTheReset()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await SeedEnrolledMemberAsync(stack, WorkspaceRole.Member);
        using var asOwner = As(stack, stack.OwnerId);

        var result = await TrySendAsync(asOwner, new ResetMemberMfaCommand(
            stack.WorkspaceId, member, await ProofAsync(stack.OwnerId, stack.WorkspaceId, member)));

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors));
        (await _host.ReadAsync(c => c.UserMfaMethods.CountAsync(m => m.UserId == member && m.Status == MfaMethodStatus.Active))).Should().Be(0);
        (await _host.ReadAsync(c => c.MfaRecoveryBatches.CountAsync(b => b.UserId == member && b.InvalidatedAt == null))).Should().Be(0);
        (await _host.ReadAsync(c => c.Sessions.CountAsync(s => s.UserId == member && s.Status == SessionStatus.Active))).Should().Be(0);
        _activity.Verify(a => a.Record(member, Notrelix.Domain.Identity.Security.SecurityActivityType.MfaReset, It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task AnAdmin_CannotResetAnyone_EvenWithAProofOfTheirOwn()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await SeedEnrolledMemberAsync(stack, WorkspaceRole.Member);
        using var asAdmin = As(stack, admin);
        var command = new ResetMemberMfaCommand(stack.WorkspaceId, member, await ProofAsync(admin, stack.WorkspaceId, member));

        var act = () => TrySendAsync(asAdmin, command);

        await act.Should().ThrowAsync<Exception>();
        await AssertUntouchedAsync(member);
    }

    [Fact]
    public async Task AProofBoundToAnotherMember_IsRefused_AndNothingChanges()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await SeedEnrolledMemberAsync(stack, WorkspaceRole.Member);
        var other = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Other");
        using var asOwner = As(stack, stack.OwnerId);

        var result = await TrySendAsync(asOwner, new ResetMemberMfaCommand(
            stack.WorkspaceId, member, await ProofAsync(stack.OwnerId, stack.WorkspaceId, other)));

        result.Succeeded.Should().BeFalse();
        await AssertUntouchedAsync(member);
    }

    [Fact]
    public async Task AStaleOrSpentProof_IsRefused_AndNothingChanges()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await SeedEnrolledMemberAsync(stack, WorkspaceRole.Member);
        using var asOwner = As(stack, stack.OwnerId);
        var proof = await ProofAsync(stack.OwnerId, stack.WorkspaceId, member);
        _host.Now = _host.Now.AddHours(1);

        var result = await TrySendAsync(asOwner, new ResetMemberMfaCommand(stack.WorkspaceId, member, proof));

        result.Succeeded.Should().BeFalse();
        await AssertUntouchedAsync(member);
    }

    [Fact]
    public async Task AnOwnersFactors_CannotBeReset_ByAnyone()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var otherOwner = await SeedEnrolledMemberAsync(stack, WorkspaceRole.Owner);
        using var asOwner = As(stack, stack.OwnerId);
        var command = new ResetMemberMfaCommand(stack.WorkspaceId, otherOwner, await ProofAsync(stack.OwnerId, stack.WorkspaceId, otherOwner));

        var act = () => TrySendAsync(asOwner, command);

        await act.Should().ThrowAsync<Exception>();
        await AssertUntouchedAsync(otherOwner);
    }
}
