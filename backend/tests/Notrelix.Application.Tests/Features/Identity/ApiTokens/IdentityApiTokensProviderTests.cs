using Notrelix.Application.Features.Identity.ApiTokens.Services;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Tests.Features.Identity.ApiTokens;

/// <summary>The person who leaves a workspace takes their tokens with them: only their active tokens in that workspace stop.</summary>
public class IdentityApiTokensProviderTests : IdentityHandlerTestBase
{
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid OtherWorkspaceId = Guid.CreateVersion7();
    private static readonly Guid Admin = Guid.CreateVersion7();

    private ApiToken Token(Guid userId, Guid workspaceId, string hash) =>
        ApiToken.Create(AccountId, workspaceId, userId, "Token", hash, null, userId, TestNow, TestNow.AddDays(30));

    private IdentityApiTokensProvider Sut() => new(IdentityContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task RevokeForMember_StopsThePersonsActiveTokensInThatWorkspaceOnly()
    {
        var leaver = Token(TestUserId, WorkspaceId, "h1");
        var leaverElsewhere = Token(TestUserId, OtherWorkspaceId, "h2");
        var colleague = Token(Guid.CreateVersion7(), WorkspaceId, "h3");
        SetupApiTokens(leaver, leaverElsewhere, colleague);

        var revoked = await Sut().RevokeForMemberAsync(
            TestUserId, WorkspaceId, Admin, ApiTokenRevokedReason.MemberRemoved, CancellationToken.None);

        revoked.Should().Be(1);
        leaver.Status.Should().Be(ApiTokenStatus.Revoked);
        leaver.RevokedReason.Should().Be(ApiTokenRevokedReason.MemberRemoved);
        leaver.RevokedBy.Should().Be(Admin);
        leaver.RevokedAt.Should().Be(TestNow);
        leaverElsewhere.Status.Should().Be(ApiTokenStatus.Active, "the person is still a member of the other workspace");
        colleague.Status.Should().Be(ApiTokenStatus.Active);
    }

    [Fact]
    public async Task RevokeForMember_RunTwice_RevokesNothingMoreAndKeepsTheFirstReason()
    {
        var leaver = Token(TestUserId, WorkspaceId, "h1");
        SetupApiTokens(leaver);
        await Sut().RevokeForMemberAsync(TestUserId, WorkspaceId, Admin, ApiTokenRevokedReason.MemberSuspended, CancellationToken.None);
        var version = leaver.Version;

        var again = await Sut().RevokeForMemberAsync(TestUserId, WorkspaceId, Admin, ApiTokenRevokedReason.MemberRemoved, CancellationToken.None);

        again.Should().Be(0);
        leaver.RevokedReason.Should().Be(ApiTokenRevokedReason.MemberSuspended);
        leaver.Version.Should().Be(version);
    }

    [Fact]
    public async Task RevokeForMember_WithNoTokens_IsANoOp()
    {
        SetupApiTokens();

        var revoked = await Sut().RevokeForMemberAsync(
            TestUserId, WorkspaceId, Admin, ApiTokenRevokedReason.MemberLeft, CancellationToken.None);

        revoked.Should().Be(0);
    }
}
