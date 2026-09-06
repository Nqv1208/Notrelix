using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Identity.Public.Facts;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.AcceptInvitation;

/// <summary>
/// Application-level orchestration base for the AcceptInvitation use case.
/// Supplies the four public ports mocked at the Application boundary plus
/// helpers that build a deterministically-valid invitation, a matching
/// presented token, and canonical default user/account facts. Invalid and
/// lifecycle-shifted variants are created per-test to prove each gate.
/// </summary>
public abstract class AcceptInvitationHandlerTestBase : WorkspaceHandlerTestBase
{
    protected const string TestEmail = "accept@example.com";
    protected const string OtherEmail = "someone-else@example.com";

    protected readonly Mock<IIdentityUserFacts> IdentityUserFactsMock = new();
    protected readonly Mock<IAccountMembershipActions> AccountMembershipActionsMock = new();
    protected readonly Mock<IAccountMembershipFacts> AccountMembershipFactsMock = new();
    protected readonly Mock<IOneTimeTokenService> OneTimeTokenServiceMock = new();

    /// <summary>A syntactically-valid SHA-256 hex digest (64 lower-case hex chars).</summary>
    protected const string ValidTokenHash =
        "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

    /// <summary>A different-but-valid digest used to prove a token-hash mismatch lookup.</summary>
    protected const string OtherTokenHash =
        "2c26b46b68ffc68ff99b453c1d30413413422d706483bfa0f98a5e886266e7ae";

    protected AcceptInvitationHandlerTestBase()
    {
        RequestContextMock.Setup(c => c.IsAuthenticated).Returns(true);
        RequestContextMock.Setup(c => c.UserId).Returns(TestUserId);

        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserFact());

        AccountMembershipFactsMock
            .Setup(f => f.GetAdmissionAsync(TestAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountMembershipAdmissionFact(CanAdmitMember: true));

        AccountMembershipActionsMock
            .Setup(a => a.EnsureWorkspaceInviteeMembershipAsync(
                TestAccountId, TestUserId, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        OneTimeTokenServiceMock
            .Setup(t => t.ParseAndHash(ValidInviteToken, TokenPurpose.WorkspaceInvitation))
            .Returns(new ParsedOneTimeToken(ValidTokenHash, HashVersion: 1));
    }

    protected const string ValidInviteToken = "v1.valid-token";

    protected IdentityUserFact CreateUserFact(
        bool emailConfirmed = true,
        bool canParticipate = true,
        string email = TestEmail)
        => new(TestUserId, email, emailConfirmed, canParticipate);

    protected WorkspaceInvitation CreateInvitation(
        Guid? workspaceId = null,
        WorkspaceRole role = WorkspaceRole.Member,
        string email = TestEmail,
        string tokenHash = ValidTokenHash,
        int hashVersion = 1,
        Guid? invitedBy = null,
        DateTimeOffset? createdAt = null,
        TimeSpan? expiry = null)
    {
        return WorkspaceInvitation.Create(
            TestAccountId,
            workspaceId ?? TestWorkspaceId,
            email,
            role,
            InvitationTokenHash.Create(tokenHash),
            hashVersion,
            invitedBy ?? TestUserId,
            createdAt ?? TestNow.AddDays(-1),
            expiry ?? TimeSpan.FromDays(7));
    }

    protected AcceptInvitationCommandHandler CreateSut()
    {
        return new AcceptInvitationCommandHandler(
            DbContextMock.Object,
            IdentityUserFactsMock.Object,
            AccountMembershipActionsMock.Object,
            AccountMembershipFactsMock.Object,
            OneTimeTokenServiceMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object,
            GrantProjectionMock.Object);
    }
}