using Notrelix.Application.Features.Identity.Ports.Bootstrap;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Queries;

/// <summary>
/// The Account a member acts in is chosen by the member's facts alone. Each tie-break has its own test, and
/// one test feeds every ordering of the same facts to show the answer does not follow the database's row order.
/// </summary>
public class BootstrapAccountChoiceTests
{
    private static readonly Guid LowId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid HighId = Guid.Parse("00000000-0000-0000-0000-000000000009");

    private static BootstrapAccountMembership Member(Guid id, AccountRole role, string name) => new(id, role, name);

    private static BootstrapWorkspaceProjection Workspace(Guid id, Guid accountId) =>
        new(id, "ws", "ws", accountId);

    [Fact]
    public void TheDefaultWorkspacesAccountWinsOverAnOwnedAccount()
    {
        var owned = Guid.NewGuid();
        var viaDefault = Guid.NewGuid();
        var defaultWorkspace = Guid.NewGuid();

        var chosen = BootstrapAccountChoice.Choose(
            defaultWorkspace,
            [Workspace(defaultWorkspace, viaDefault)],
            [Member(owned, AccountRole.Owner, "Aardvark")]);

        chosen.Should().Be(viaDefault);
    }

    [Fact]
    public void ADefaultWorkspaceThePersonDoesNotBelongToIsIgnored()
    {
        var owned = Guid.NewGuid();

        var chosen = BootstrapAccountChoice.Choose(
            Guid.NewGuid(),
            [Workspace(Guid.NewGuid(), Guid.NewGuid())],
            [Member(owned, AccountRole.Owner, "Aardvark")]);

        chosen.Should().Be(owned, "a default Workspace that is not among the person's own Workspaces names nothing");
    }

    [Fact]
    public void AnOwnedAccountBeatsAnAdminAccountEvenWhenItsNameSortsLater()
    {
        var admin = Guid.NewGuid();
        var owner = Guid.NewGuid();

        var chosen = BootstrapAccountChoice.Choose(
            null,
            [],
            [Member(admin, AccountRole.Admin, "Aardvark"), Member(owner, AccountRole.Owner, "Zebra")]);

        chosen.Should().Be(owner);
    }

    [Fact]
    public void BetweenTwoOwnedAccountsTheNameDecidesWithoutRegardToCase()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();

        var chosen = BootstrapAccountChoice.Choose(
            null,
            [],
            [Member(beta, AccountRole.Owner, "beta co"), Member(alpha, AccountRole.Owner, "ALPHA Co")]);

        chosen.Should().Be(alpha);
    }

    [Fact]
    public void BetweenAccountsWithTheSameRoleAndNameTheLowestIdDecides()
    {
        var chosen = BootstrapAccountChoice.Choose(
            null,
            [],
            [Member(HighId, AccountRole.Owner, "Same"), Member(LowId, AccountRole.Owner, "Same")]);

        chosen.Should().Be(LowId);
    }

    [Fact]
    public void WithoutAnOwnedAccountAnyOtherMembershipIsChosenByNameThenId()
    {
        var admin = Guid.NewGuid();
        var member = Guid.NewGuid();

        var chosen = BootstrapAccountChoice.Choose(
            null,
            [],
            [Member(member, AccountRole.Member, "Zulu"), Member(admin, AccountRole.Admin, "Alpha")]);

        chosen.Should().Be(admin, "among non-owner roles the name decides, not the role");
    }

    [Fact]
    public void WithoutMembershipsTheLowestAccountIdOfTheMembersWorkspacesIsChosen()
    {
        var chosen = BootstrapAccountChoice.Choose(
            null,
            [Workspace(Guid.NewGuid(), HighId), Workspace(Guid.NewGuid(), LowId)],
            []);

        chosen.Should().Be(LowId);
    }

    [Fact]
    public void WithNothingAtAllNoAccountIsChosen()
    {
        BootstrapAccountChoice.Choose(null, [], []).Should().BeNull();
        BootstrapAccountChoice.Own([]).Should().BeNull();
    }

    [Fact]
    public void TheMembersOwnAccountPrefersAnOwnedOneThenAnyInTheSameOrder()
    {
        var owner = Guid.NewGuid();

        BootstrapAccountChoice
            .Own([Member(Guid.NewGuid(), AccountRole.Admin, "Aardvark"), Member(owner, AccountRole.Owner, "Zebra")])
            .Should().Be(owner);
        BootstrapAccountChoice
            .Own([Member(HighId, AccountRole.Member, "Same"), Member(LowId, AccountRole.Admin, "Same")])
            .Should().Be(LowId);
    }

    [Fact]
    public void EveryOrderingOfTheSameFactsGivesTheSameAccount()
    {
        BootstrapAccountMembership[] memberships =
        [
            Member(Guid.Parse("00000000-0000-0000-0000-0000000000a1"), AccountRole.Member, "Alpha"),
            Member(Guid.Parse("00000000-0000-0000-0000-0000000000b2"), AccountRole.Owner, "Gamma"),
            Member(Guid.Parse("00000000-0000-0000-0000-0000000000c3"), AccountRole.Owner, "beta"),
            Member(Guid.Parse("00000000-0000-0000-0000-0000000000d4"), AccountRole.Owner, "Beta"),
            Member(Guid.Parse("00000000-0000-0000-0000-0000000000e5"), AccountRole.Admin, "Aardvark"),
        ];
        BootstrapWorkspaceProjection[] workspaces =
        [
            Workspace(Guid.NewGuid(), Guid.Parse("00000000-0000-0000-0000-0000000000f6")),
            Workspace(Guid.NewGuid(), Guid.Parse("00000000-0000-0000-0000-0000000000f7")),
        ];

        // "Beta" and "beta" are the same name ignoring case, so the lower id (c3) decides between them.
        var expected = Guid.Parse("00000000-0000-0000-0000-0000000000c3");
        var random = new Random(20261122);
        for (var round = 0; round < 50; round++)
        {
            var shuffled = memberships.OrderBy(_ => random.Next()).ToArray();
            var shuffledWorkspaces = workspaces.OrderBy(_ => random.Next()).ToArray();

            BootstrapAccountChoice.Choose(null, shuffledWorkspaces, shuffled).Should().Be(expected);
            BootstrapAccountChoice.Own(shuffled).Should().Be(expected);
        }
    }
}
