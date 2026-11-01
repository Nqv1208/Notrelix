using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// The caller's effective level on a Board, computed by the Governance engine
/// from the same <see cref="AccessFacts"/> the single-read decision uses: the
/// level names the strongest source (workspace Owner, explicit grant, board
/// membership, Space level, workspace default), and the function reports no
/// access exactly when the engine denies <c>ViewBoard</c>.
/// </summary>
public sealed class EffectiveBoardAccessTests
{
    private static readonly RequestDescriptor ViewBoardDescriptor =
        RequestDescriptorValidator.Create(typeof(GetBoardQuery));

    [Fact]
    public void WorkspaceOwner_IsManager_FromTheOwnerSource()
    {
        var access = AccessPolicyEngine.EvaluateEffectiveBoardAccess(Facts("Owner", audience: "Private"));

        access.Basis.Should().Be(EffectiveAccessBasis.WorkspaceOwner);
        access.Level.Should().Be("Manager");
    }

    [Fact]
    public void ExplicitCommenterGrant_OnAPrivateBoard_IsCommenter()
    {
        var access = AccessPolicyEngine.EvaluateEffectiveBoardAccess(
            Facts("Member", audience: "Private", explicitRank: 2));

        access.Basis.Should().Be(EffectiveAccessBasis.ResourcePermission);
        access.Level.Should().Be("Commenter");
    }

    [Fact]
    public void SpaceMember_OnAWorkspaceBoardInTheSpace_IsEditor_FromTheSpace()
    {
        var access = AccessPolicyEngine.EvaluateEffectiveBoardAccess(
            Facts("Member", spaceId: Guid.NewGuid(), spaceRole: "Member"));

        access.Basis.Should().Be(EffectiveAccessBasis.Space);
        access.Level.Should().Be("Editor");
    }

    [Fact]
    public void WorkspaceAdmin_OnAPlacedWorkspaceBoard_IsManager_FromWorkspaceAdmin()
    {
        var access = AccessPolicyEngine.EvaluateEffectiveBoardAccess(
            Facts("Admin", spaceId: Guid.NewGuid()));

        access.Basis.Should().Be(EffectiveAccessBasis.WorkspaceAdmin);
        access.Level.Should().Be("Manager");
    }

    [Fact]
    public void WorkspaceAdmin_NeverReachesAPrivateBoard()
    {
        var access = AccessPolicyEngine.EvaluateEffectiveBoardAccess(
            Facts("Admin", audience: "Private", spaceId: Guid.NewGuid()));

        access.Basis.Should().Be(EffectiveAccessBasis.None);
        access.Level.Should().BeNull();
    }

    [Fact]
    public void HigherOfExplicitAndSpace_Wins_AndTheExplicitGrantIsTheSourceOnATie()
    {
        var spaceWins = AccessPolicyEngine.EvaluateEffectiveBoardAccess(
            Facts("Member", spaceId: Guid.NewGuid(), spaceRole: "Admin", explicitRank: 2));
        spaceWins.Basis.Should().Be(EffectiveAccessBasis.Space);
        spaceWins.Level.Should().Be("Manager");

        var tie = AccessPolicyEngine.EvaluateEffectiveBoardAccess(
            Facts("Member", spaceId: Guid.NewGuid(), spaceRole: "Member", explicitRank: 3));
        tie.Basis.Should().Be(EffectiveAccessBasis.ResourcePermission);
        tie.Level.Should().Be("Editor");
    }

    [Fact]
    public void SpaceRoles_NeverReachAPrivateBoard_ButTheExplicitGrantDoes()
    {
        var access = AccessPolicyEngine.EvaluateEffectiveBoardAccess(
            Facts("Member", audience: "Private", spaceId: Guid.NewGuid(), spaceRole: "Admin", explicitRank: 1));

        access.Basis.Should().Be(EffectiveAccessBasis.ResourcePermission);
        access.Level.Should().Be("Viewer");
    }

    [Theory]
    [InlineData("Member", "Editor")]
    [InlineData("Admin", "Manager")]
    [InlineData("Owner", "Manager")]
    [InlineData("Observer", "Viewer")]
    [InlineData("Guest", "Viewer")]
    public void BoardMembershipWithoutAGrant_IsLevelledByTheBoardRole(string boardRole, string level)
    {
        var access = AccessPolicyEngine.EvaluateEffectiveBoardAccess(
            Facts("Member", audience: "Private", boardRole: boardRole));

        access.Basis.Should().Be(EffectiveAccessBasis.BoardMembership);
        access.Level.Should().Be(level);
    }

    [Fact]
    public void UnplacedWorkspaceBoard_GivesMembersTheWorkspaceDefault_AndGuestsNothing()
    {
        var member = AccessPolicyEngine.EvaluateEffectiveBoardAccess(Facts("Member"));
        member.Basis.Should().Be(EffectiveAccessBasis.WorkspaceDefault);
        member.Level.Should().Be("Editor");

        AccessPolicyEngine.EvaluateEffectiveBoardAccess(Facts("Guest")).Basis
            .Should().Be(EffectiveAccessBasis.None, "Guests need explicit access (GOV-025 step 6)");
    }

    [Fact]
    public void Guest_WithAViewerGrant_HasTheGrantLevel()
    {
        var access = AccessPolicyEngine.EvaluateEffectiveBoardAccess(Facts("Guest", explicitRank: 1));

        access.Basis.Should().Be(EffectiveAccessBasis.ResourcePermission);
        access.Level.Should().Be("Viewer");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Guest_IsViewOnly_WhateverTheGrant(int explicitRank)
    {
        var access = AccessPolicyEngine.EvaluateEffectiveBoardAccess(Facts("Guest", explicitRank: explicitRank));

        access.Basis.Should().Be(EffectiveAccessBasis.ResourcePermission);
        access.Level.Should().Be("Viewer", "a person outside the workspace reads what was shared with them (GOV-030)");
    }

    [Fact]
    public void FirstPriorityDenyRule_RemovesAccess_AndAnAllowRuleAloneGivesViewer()
    {
        AccessPolicyEngine.EvaluateEffectiveBoardAccess(
                Facts("Member", explicitRank: 4, rules: [new AccessPermissionRule(0, "Deny")]))
            .Basis.Should().Be(EffectiveAccessBasis.None);

        var allowed = AccessPolicyEngine.EvaluateEffectiveBoardAccess(
            Facts("Member", audience: "Private", rules: [new AccessPermissionRule(0, "Allow")]));
        allowed.Basis.Should().Be(EffectiveAccessBasis.PermissionRule);
        allowed.Level.Should().Be("Viewer");
    }

    [Fact]
    public void MissingResource_OrNoWorkspaceMembership_IsNoAccess()
    {
        AccessPolicyEngine.EvaluateEffectiveBoardAccess(Facts("Owner", exists: false)).Basis
            .Should().Be(EffectiveAccessBasis.None);
        AccessPolicyEngine.EvaluateEffectiveBoardAccess(Facts(null)).Basis
            .Should().Be(EffectiveAccessBasis.None);
    }

    /// <summary>
    /// Parity: across the workspace role × audience × placement × Space role ×
    /// board membership × grant × rule matrix, the effective access is "none"
    /// exactly when the engine's single read (<c>ViewBoard</c>) is not allowed.
    /// </summary>
    [Fact]
    public void NoAccess_EqualsTheSingleReadDenial_AcrossTheMatrix()
    {
        var engine = new AccessPolicyEngine();
        var context = new ExecutionContextSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");
        var request = new GetBoardQuery(Guid.NewGuid());
        var checkedCases = 0;

        foreach (var role in new[] { "Owner", "Admin", "Member", "Guest", null })
            foreach (var audience in new[] { "Workspace", "Private" })
                foreach (var placed in new[] { false, true })
                    foreach (var spaceRole in new[] { null, "Viewer", "Member", "Admin" })
                        foreach (var open in new[] { false, true })
                            foreach (var boardRole in new[] { null, "Member", "Observer" })
                                foreach (var explicitRank in new int?[] { null, 0, 1, 2, 4 })
                                    foreach (var rule in new[] { null, "Allow", "Deny" })
                                    {
                                        if (!placed && (spaceRole is not null || open))
                                            continue;

                                        var facts = Facts(
                                            role, audience: audience, spaceId: placed ? Guid.NewGuid() : null, spaceRole: spaceRole,
                                            spaceIsOpen: open, boardRole: boardRole, explicitRank: explicitRank,
                                            rules: rule is null ? [] : [new AccessPermissionRule(0, rule)]);

                                        var allowed = engine.Evaluate(ViewBoardDescriptor, context, facts, request).Kind == AccessDecisionKind.Allowed;
                                        var effective = AccessPolicyEngine.EvaluateEffectiveBoardAccess(facts);

                                        (effective.Basis != EffectiveAccessBasis.None).Should().Be(allowed,
                                            $"role={role ?? "-"} audience={audience} placed={placed} spaceRole={spaceRole ?? "-"} open={open} " +
                                            $"boardRole={boardRole ?? "-"} explicit={explicitRank?.ToString() ?? "-"} rule={rule ?? "-"}");
                                        if (effective.Basis != EffectiveAccessBasis.None)
                                            effective.Level.Should().NotBeNull();
                                        checkedCases++;
                                    }

        checkedCases.Should().BeGreaterThan(1000, "the parity sweep must evaluate the real matrix (NRX-016)");
    }

    private static AccessFacts Facts(
        string? workspaceRole,
        string audience = "Workspace",
        Guid? spaceId = null,
        string? spaceRole = null,
        bool spaceIsOpen = false,
        string? boardRole = null,
        int? explicitRank = null,
        AccessPermissionRule[]? rules = null,
        bool exists = true) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: workspaceRole,
        ResourceExists: exists,
        ResourceAudience: exists ? audience : null,
        ResourceMemberRole: boardRole,
        HasExplicitResourcePermission: explicitRank is not null,
        PermissionRules: rules ?? [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: explicitRank,
        TargetPermissionRank: null)
        {
            ResourceSpaceId = spaceId,
            SpaceExists = spaceId is not null,
            SpaceRole = spaceRole,
            SpaceIsOpen = spaceIsOpen,
        };
}
