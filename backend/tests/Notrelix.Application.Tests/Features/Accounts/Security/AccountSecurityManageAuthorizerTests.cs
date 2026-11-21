using Notrelix.Application.Features.Accounts.Security;
using Notrelix.Application.Features.Accounts.Security.Commands.SetAccountMfaPolicy;

namespace Notrelix.Application.Tests.Features.Accounts.Security;

/// <summary>
/// The CanManage flag of the security policy read comes from the real AccessPolicyEngine with the real set-policy
/// descriptor, so it is true exactly where the write is allowed: the Account Owner, never an Admin or a Member.
/// </summary>
public sealed class AccountSecurityManageAuthorizerTests
{
    private static readonly RequestDescriptor Descriptor = RequestDescriptorValidator.Create(typeof(SetAccountMfaPolicyCommand));

    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Admin", false)]
    [InlineData("Member", false)]
    public async Task CanManage_IsTrueOnlyForTheAccountOwner(string role, bool expected)
    {
        var snapshot = new ExecutionContextSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, Guid.NewGuid().ToString("D"));
        var facts = new AccessFacts(
            UserExists: true, EmailVerified: true, AccountExists: true, AccountMemberRole: role,
            WorkspaceExists: false, WorkspaceMemberRole: null, ResourceExists: false, ResourceAudience: null,
            ResourceMemberRole: null, HasExplicitResourcePermission: false, PermissionRules: [],
            SubscriptionRequirementSatisfied: false, FeatureEnabled: false, ActiveResourcePermissionRank: null,
            TargetPermissionRank: null);
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(d => d.GetRequired(typeof(SetAccountMfaPolicyCommand))).Returns(Descriptor);
        var provider = new Mock<IAccessFactsProvider>();
        provider.Setup(p => p.ResolveAsync(Descriptor, snapshot, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(facts);
        var reader = new Mock<IExecutionContextReader>();
        reader.SetupGet(r => r.Snapshot).Returns(snapshot);

        var sut = new AccountSecurityManageAuthorizer(descriptors.Object, provider.Object, new AccessPolicyEngine(), reader.Object);

        (await sut.CanManageAsync(CancellationToken.None)).Should().Be(expected);
    }
}
