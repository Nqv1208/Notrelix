using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMembers;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Workspaces;
using DomainRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

public class InviteMembersCommandHandlerTests
{
    private readonly Mock<IRequestHandler<InviteMemberCommand, Result<Guid>>> _single = new();
    private readonly Mock<IRequestDescriptorRegistry> _descriptors = new();
    private readonly Mock<IAccessFactsProvider> _facts = new();
    private readonly Mock<IAccessPolicyEvaluator> _policy = new();
    private readonly Mock<IExecutionContextReader> _context = new();
    private readonly Guid _workspaceId = Guid.NewGuid();

    private InviteMembersCommandHandler Sut(AccessDecision guestDecision)
    {
        _context.Setup(c => c.Snapshot).Returns(new ExecutionContextSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), _workspaceId, null, ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace, "test"));
        _descriptors.Setup(d => d.GetRequired(typeof(InviteMemberCommand))).Returns((RequestDescriptor)null!);
        _policy.Setup(p => p.Evaluate(It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(), It.IsAny<AccessFacts>(), It.IsAny<object>()))
            .Returns(guestDecision);
        return new InviteMembersCommandHandler(_single.Object, _descriptors.Object, _facts.Object, _policy.Object, _context.Object);
    }

    [Fact]
    public async Task EachSingleRefusalBecomesThatAddressesOwnOutcome_AndTheOthersAreStillSent()
    {
        var sentId = Guid.NewGuid();
        _single.Setup(h => h.Handle(It.IsAny<InviteMemberCommand>(), It.IsAny<CancellationToken>()))
            .Returns((InviteMemberCommand c, CancellationToken _) => c.Email switch
            {
                "member@x.com" => throw new ConflictException("m", InviteMemberCommandHandler.AlreadyMemberErrorCode),
                "pending@x.com" => throw new ConflictException("p", InviteMemberCommandHandler.PendingInvitationExistsErrorCode),
                "admin@x.com" => throw new DomainRuleException(WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsAdmin, "no"),
                _ => Task.FromResult(Result<Guid>.Success(sentId)),
            });

        var result = await Sut(AccessDecision.Allow()).Handle(new InviteMembersCommand(_workspaceId,
        [
            new("member@x.com", "Member"), new("pending@x.com", "Member"), new("bad", "Member"),
            new("admin@x.com", "Member"), new("fresh@x.com", "Member"), new("role@x.com", "Wizard"),
        ]), CancellationToken.None);

        var rows = result.Data!.Outcomes.ToDictionary(o => o.Email);
        rows["member@x.com"].Outcome.Should().Be(MemberInvitationOutcomes.AlreadyMember);
        rows["pending@x.com"].Outcome.Should().Be(MemberInvitationOutcomes.AlreadyPending);
        rows["bad"].Outcome.Should().Be(MemberInvitationOutcomes.Invalid);
        rows["role@x.com"].Outcome.Should().Be(MemberInvitationOutcomes.Invalid);
        rows["admin@x.com"].Should().Match<MemberInvitationOutcomeDto>(o => o.Outcome == MemberInvitationOutcomes.Refused && o.Code == WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsAdmin);
        rows["fresh@x.com"].Should().Match<MemberInvitationOutcomeDto>(o => o.Outcome == MemberInvitationOutcomes.Sent && o.InvitationId == sentId);
    }

    [Fact]
    public async Task GuestsAreRefusedOneByOneWhenTheEngineSaysGuestInvitesAreOff_AndMembersStillGo()
    {
        _single.Setup(h => h.Handle(It.IsAny<InviteMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));
        var off = AccessDecision.Deny(AccessDecisionKind.Forbidden, "off", AccessDenialCodes.GuestInvitesOff);

        var result = await Sut(off).Handle(new InviteMembersCommand(_workspaceId,
            [new("a@x.com", "Member"), new("g1@x.com", "Guest"), new("b@x.com", "member"), new("g2@x.com", "guest")]),
            CancellationToken.None);

        result.Data!.Outcomes.Where(o => o.Outcome == MemberInvitationOutcomes.Sent).Select(o => o.Email).Should().Equal("a@x.com", "b@x.com");
        result.Data.Outcomes.Where(o => o.Outcome == MemberInvitationOutcomes.Refused).Should()
            .HaveCount(2).And.OnlyContain(o => o.Code == AccessDenialCodes.GuestInvitesOff);
        _single.Verify(h => h.Handle(It.Is<InviteMemberCommand>(c => c.Role == WorkspaceRole.Guest), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ARepeatedAddressIsInvitedOnce()
    {
        _single.Setup(h => h.Handle(It.IsAny<InviteMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        var result = await Sut(AccessDecision.Allow()).Handle(new InviteMembersCommand(_workspaceId,
            [new("Ana@X.com", "Member"), new(" ana@x.com ", "Member")]), CancellationToken.None);

        result.Data!.Outcomes.Should().ContainSingle();
        _single.Verify(h => h.Handle(It.IsAny<InviteMemberCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OwnerAndAdminEntries_AreRefusedByTheDomainOneByOne_AndTheValidEntriesStillGo()
    {
        var sentId = Guid.NewGuid();
        _single.Setup(h => h.Handle(It.IsAny<InviteMemberCommand>(), It.IsAny<CancellationToken>()))
            .Returns((InviteMemberCommand c, CancellationToken _) => c.Role switch
            {
                WorkspaceRole.Owner => throw new DomainRuleException(WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsOwner, "no"),
                WorkspaceRole.Admin => throw new DomainRuleException(WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsAdmin, "no"),
                _ => Task.FromResult(Result<Guid>.Success(sentId)),
            });

        var result = await Sut(AccessDecision.Allow()).Handle(new InviteMembersCommand(_workspaceId,
            [new("owner@x.com", "Owner"), new("a@x.com", "Member"), new("admin@x.com", "admin"), new("g@x.com", "Guest")]),
            CancellationToken.None);

        var rows = result.Data!.Outcomes.ToDictionary(o => o.Email);
        rows["owner@x.com"].Should().Match<MemberInvitationOutcomeDto>(o => o.Outcome == MemberInvitationOutcomes.Refused && o.Code == WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsOwner && o.InvitationId == null);
        rows["admin@x.com"].Should().Match<MemberInvitationOutcomeDto>(o => o.Outcome == MemberInvitationOutcomes.Refused && o.Code == WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsAdmin && o.InvitationId == null);
        rows["a@x.com"].Outcome.Should().Be(MemberInvitationOutcomes.Sent);
        rows["g@x.com"].Outcome.Should().Be(MemberInvitationOutcomes.Sent);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData(" 3 ")]
    [InlineData("-1")]
    [InlineData("Member,Guest")]
    public async Task ARoleGivenAsANumberOrAList_IsInvalid_AndNeverReachesTheInvitation(string role)
    {
        var result = await Sut(AccessDecision.Allow()).Handle(new InviteMembersCommand(_workspaceId, [new("n@x.com", role)]), CancellationToken.None);

        result.Data!.Outcomes.Should().ContainSingle().Which.Outcome.Should().Be(MemberInvitationOutcomes.Invalid);
        _single.Verify(h => h.Handle(It.IsAny<InviteMemberCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
