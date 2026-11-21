using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The production step-up service over an in-memory single-use proof store: proofs are issued and spent through
/// the real <see cref="SecurityStepUpService"/> (user + session + purpose binding, expiry, single use); only the
/// factor verification dependencies, which the issue and spend paths never reach, are stand-ins.
/// </summary>
internal sealed class HandoverStepUp
{
    public HandoverStepUp(IDateTimeProvider clock)
    {
        Service = new SecurityStepUpService(
            new Mock<IIdentityDbContext>().Object,
            new Mock<IMfaChallengeStore>().Object,
            new InMemoryStepUpProofStore(clock),
            new Mock<IMfaCodeVerifier>().Object,
            new Mock<IPasswordHasher>().Object,
            clock,
            new Mock<ILoginThrottle>().Object);
    }

    public ISecurityStepUpService Service { get; }

    /// <summary>A proof for the handover of <paramref name="workspaceId"/> to <paramref name="newOwnerId"/>; spent only for exactly that.</summary>
    public async Task<string> ProofAsync(
        Guid userId, Guid sessionId, Guid workspaceId, Guid newOwnerId,
        StepUpPurpose purpose = StepUpPurpose.TransferWorkspaceOwnership)
    {
        var granted = await Service.GrantOAuthProofAsync(
            userId, sessionId, purpose, StepUpTarget.OwnershipTransfer(workspaceId, newOwnerId), CancellationToken.None);
        granted.Succeeded.Should().BeTrue();
        return granted.Data!.ProofToken;
    }
}
