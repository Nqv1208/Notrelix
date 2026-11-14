using FluentAssertions;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Domain.Tests.Chat.Spaces;

/// <summary>Direct and group conversations (UC-CHT-E01 to E04, E07; CHT-003, CHT-014): the aggregate's rules, no handler involved.</summary>
public class ConversationTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly Guid _carol = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateDirect_MakesTwoMembersAndNoName()
    {
        var space = Space.CreateDirect(_accountId, _workspaceId, _alice, _bob, _now);

        space.Kind.Should().Be(SpaceKind.Direct);
        space.IsConversation.Should().BeTrue();
        space.IsPublic.Should().BeFalse();
        space.Name.Should().BeEmpty();
        space.Memberships.Where(m => m.IsActive).Select(m => m.UserId).Should().BeEquivalentTo([_alice, _bob]);
        space.Memberships.Should().OnlyContain(m => m.Role == MembershipRole.Member);
    }

    [Fact]
    public void CreateDirect_RefusesTheSamePersonTwice()
    {
        var act = () => Space.CreateDirect(_accountId, _workspaceId, _alice, _alice, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SelfTarget);
    }

    [Fact]
    public void CreateGroup_NeedsThreePeopleCountingTheCaller()
    {
        var act = () => Space.CreateGroup(_accountId, _workspaceId, "Trio", [_bob, _carol], _alice, _now);
        act.Should().NotThrow("two others plus the caller is three");

        var tooFew = () => Space.CreateGroup(_accountId, _workspaceId, "Duo", [_bob], _alice, _now);
        tooFew.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.GroupTooSmall);
    }

    [Fact]
    public void CreateGroup_AddsTheCallerAndIgnoresDuplicates()
    {
        var space = Space.CreateGroup(_accountId, _workspaceId, "  Team  ", [_bob, _bob, _carol], _alice, _now);

        space.Name.Should().Be("Team");
        space.ActiveMemberCount().Should().Be(3);
        space.IsActiveMember(_alice).Should().BeTrue();
    }

    [Fact]
    public void CreateGroup_HasOptionalNameAndRefusesOverTheCap()
    {
        Space.CreateGroup(_accountId, _workspaceId, null, [_bob, _carol], _alice, _now).Name.Should().BeEmpty();

        var others = Enumerable.Range(0, ChatLimits.GroupCap).Select(_ => Guid.NewGuid()).ToList();
        var act = () => Space.CreateGroup(_accountId, _workspaceId, null, others, _alice, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.LimitReached);
    }

    [Fact]
    public void CreateGroup_TheCapCountsTheCallerSoEightIsAllowedAndNineIsNot()
    {
        var seven = Enumerable.Range(0, ChatLimits.GroupCap - 1).Select(_ => Guid.NewGuid()).ToList();
        Space.CreateGroup(_accountId, _workspaceId, null, seven, _alice, _now).ActiveMemberCount().Should().Be(ChatLimits.GroupCap);

        var eight = Enumerable.Range(0, ChatLimits.GroupCap).Select(_ => Guid.NewGuid()).ToList();
        var act = () => Space.CreateGroup(_accountId, _workspaceId, null, eight, _alice, _now);
        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.LimitReached);
    }

    [Fact]
    public void AddParticipant_AddsOnceAndStopsAtTheCap()
    {
        var group = Space.CreateGroup(_accountId, _workspaceId, "Team", [_alice, _bob, _carol], _alice, _now);
        var dave = Guid.NewGuid();

        group.AddParticipant(dave, _now).Should().BeTrue();
        group.AddParticipant(dave, _now).Should().BeFalse("already a participant");

        var others = Enumerable.Range(0, ChatLimits.GroupCap).Select(_ => Guid.NewGuid()).ToList();
        var full = Space.CreateGroup(_accountId, _workspaceId, "Full", [_alice, .. others.Take(ChatLimits.GroupCap - 2)], _alice, _now);
        full.ActiveMemberCount().Should().Be(ChatLimits.GroupCap - 1);
        full.AddParticipant(Guid.NewGuid(), _now).Should().BeTrue("the eighth person fits");
        var over = () => full.AddParticipant(Guid.NewGuid(), _now);
        over.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.LimitReached);
    }

    [Fact]
    public void AddParticipant_IsRefusedOnADirectConversation()
    {
        var direct = Space.CreateDirect(_accountId, _workspaceId, _alice, _bob, _now);

        var act = () => direct.AddParticipant(_carol, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.KindNotSupported);
    }

    [Fact]
    public void LeaveConversation_EndsTheCallersMembershipOfAGroup()
    {
        var group = Space.CreateGroup(_accountId, _workspaceId, "Team", [_alice, _bob, _carol], _alice, _now);

        group.LeaveConversation(_bob, _now.AddMinutes(1)).Should().BeTrue();
        group.IsActiveMember(_bob).Should().BeFalse();
        group.LeaveConversation(_bob, _now.AddMinutes(2)).Should().BeFalse("a second leave changes nothing");
        group.ActiveMemberCount().Should().Be(2);
    }

    [Fact]
    public void LeaveConversation_IsRefusedOnADirectConversation()
    {
        var direct = Space.CreateDirect(_accountId, _workspaceId, _alice, _bob, _now);

        var act = () => direct.LeaveConversation(_alice, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.KindNotSupported);
    }

    [Fact]
    public void CloseIfNoParticipants_ClosesOnlyAnEmptyConversation()
    {
        var group = Space.CreateGroup(_accountId, _workspaceId, "Team", [_alice, _bob, _carol], _alice, _now);
        group.LeaveConversation(_alice, _now);
        group.LeaveConversation(_bob, _now);

        group.CloseIfNoParticipants(_carol, _now).Should().BeFalse("carol is still in it");

        group.LeaveConversation(_carol, _now);
        group.CloseIfNoParticipants(_carol, _now).Should().BeTrue();
        group.IsDeleted.Should().BeTrue();
        group.CloseIfNoParticipants(_carol, _now).Should().BeFalse("closing twice changes nothing");
    }

    [Fact]
    public void Hide_IsPerMembershipAndRestoreShowsItAgain()
    {
        var direct = Space.CreateDirect(_accountId, _workspaceId, _alice, _bob, _now);
        var aliceRow = direct.GetActiveMembership(_alice)!;
        var bobRow = direct.GetActiveMembership(_bob)!;

        aliceRow.Hide(_now).Should().BeTrue();
        aliceRow.IsHidden.Should().BeTrue();
        aliceRow.Hide(_now.AddMinutes(1)).Should().BeFalse("already hidden");
        bobRow.IsHidden.Should().BeFalse("hiding is for the caller only");

        aliceRow.Restore().Should().BeTrue();
        aliceRow.IsHidden.Should().BeFalse();
        aliceRow.Restore().Should().BeFalse();
    }

    [Fact]
    public void Rejoin_ClearsTheHiddenState()
    {
        var group = Space.CreateGroup(_accountId, _workspaceId, "Team", [_alice, _bob, _carol], _alice, _now);
        group.GetActiveMembership(_bob)!.Hide(_now);
        group.LeaveConversation(_bob, _now);

        group.AddParticipant(_bob, _now.AddDays(1));

        group.GetActiveMembership(_bob)!.IsHidden.Should().BeFalse();
    }
}

public class DirectPairTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();

    [Fact]
    public void Order_PutsTheLowerIdFirstWhateverTheArgumentOrder()
    {
        var low = new Guid("00000000-0000-0000-0000-000000000001");
        var high = new Guid("ffffffff-0000-0000-0000-000000000001");

        DirectPair.Order(high, low).Should().Be((low, high));
        DirectPair.Order(low, high).Should().Be((low, high));
    }

    [Fact]
    public void Order_RefusesTheSamePerson()
    {
        var person = Guid.NewGuid();

        var act = () => DirectPair.Order(person, person);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SelfTarget);
    }

    [Fact]
    public void Create_RecordsTheOrderedPairAndTheSpace()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var space = Guid.NewGuid();

        var pair = DirectPair.Create(_accountId, _workspaceId, bob, alice, space);

        var (low, high) = DirectPair.Order(alice, bob);
        pair.UserLow.Should().Be(low);
        pair.UserHigh.Should().Be(high);
        pair.SpaceId.Should().Be(space);
        pair.WorkspaceId.Should().Be(_workspaceId);
    }
}

public class PersonBlockTests
{
    [Fact]
    public void Create_RefusesBlockingYourself()
    {
        var person = Guid.NewGuid();

        var act = () => PersonBlock.Create(Guid.NewGuid(), Guid.NewGuid(), person, person, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SelfTarget);
    }

    [Fact]
    public void Create_KeepsTheDirectionOfTheBlock()
    {
        var blocker = Guid.NewGuid();
        var blocked = Guid.NewGuid();

        var block = PersonBlock.Create(Guid.NewGuid(), Guid.NewGuid(), blocker, blocked, DateTimeOffset.UtcNow);

        block.BlockerId.Should().Be(blocker);
        block.BlockedId.Should().Be(blocked);
    }
}
