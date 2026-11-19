using FluentAssertions;
using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Calls.Events;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Domain.Tests.Chat.Calls;

/// <summary>
/// The call state machine (SPEC-CHAT-MEDIA-CALLS sections 4 and 5): every legal transition and every illegal one, the roster caps, the
/// direct and group differences, the media state, and the version and fact each change leaves.
/// </summary>
public class CallTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid SpaceId = Guid.NewGuid();
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();
    private static readonly Guid Carol = Guid.NewGuid();

    private static Call Ringing(SpaceKind space = SpaceKind.Direct, CallKind kind = CallKind.Voice) =>
        Call.Start(Account, Workspace, SpaceId, space, Alice, kind, "room-1", "ap-southeast-1", T0);

    private static Call Active(SpaceKind space = SpaceKind.Direct, CallKind kind = CallKind.Voice)
    {
        var call = Ringing(space, kind);
        call.Answer(Bob, T0.AddSeconds(5));
        return call;
    }

    private static void Refused(Action act, string code) =>
        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(code);

    [Fact]
    public void Start_rings_with_the_starter_as_the_first_participant_and_the_server_region()
    {
        var call = Ringing(kind: CallKind.Video);

        call.State.Should().Be(CallState.Ringing);
        call.EndReason.Should().BeNull();
        call.Region.Should().Be("ap-southeast-1");
        call.Version.Should().Be(1);
        call.Participants.Should().ContainSingle(p => p.PersonId == Alice && p.IsJoined);
        var fact = call.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CallChangedDomainEvent>().Subject;
        fact.State.Should().Be(CallState.Ringing);
        fact.ParticipantIds.Should().Equal(Alice);
        fact.RingsMembers.Should().BeTrue();
    }

    [Fact]
    public void A_channel_call_does_not_ring_its_members()
    {
        Ringing(SpaceKind.ChannelPublic).RingsMembers.Should().BeFalse();
        Ringing(SpaceKind.Group).RingsMembers.Should().BeTrue();
    }

    [Fact]
    public void StartBusy_is_stored_ended_with_reason_busy_and_no_participant()
    {
        var call = Call.StartBusy(Account, Workspace, SpaceId, SpaceKind.Direct, Alice, CallKind.Voice, "room-b", "ap-southeast-1", T0);

        call.State.Should().Be(CallState.Ended);
        call.EndReason.Should().Be(CallEndReason.Busy);
        call.Participants.Should().BeEmpty();
        call.EndedAt.Should().Be(T0);
    }

    [Fact]
    public void Answer_moves_ringing_to_active_and_adds_the_answerer()
    {
        var call = Ringing();

        call.Answer(Bob, T0.AddSeconds(3)).Should().BeTrue();

        call.State.Should().Be(CallState.Active);
        call.AnsweredAt.Should().Be(T0.AddSeconds(3));
        call.Version.Should().Be(2);
        call.Participants.Select(p => p.PersonId).Should().BeEquivalentTo([Alice, Bob]);
    }

    [Fact]
    public void Answer_by_someone_already_in_the_call_changes_nothing()
    {
        var call = Active();
        var version = call.Version;

        call.Answer(Bob, T0.AddSeconds(9)).Should().BeFalse();
        call.Answer(Alice, T0.AddSeconds(9)).Should().BeFalse();

        call.Version.Should().Be(version);
    }

    [Fact]
    public void Decline_ends_a_direct_call_as_declined()
    {
        var call = Ringing();

        call.Decline(Bob, T0.AddSeconds(4)).Should().BeTrue();

        call.State.Should().Be(CallState.Ended);
        call.EndReason.Should().Be(CallEndReason.Declined);
        call.Participants.Should().OnlyContain(p => !p.IsJoined);
    }

    [Fact]
    public void Decline_in_a_group_call_changes_nothing()
    {
        var call = Ringing(SpaceKind.Group);
        var version = call.Version;

        call.Decline(Bob, T0.AddSeconds(4)).Should().BeFalse();

        call.State.Should().Be(CallState.Ringing);
        call.Version.Should().Be(version);
    }

    [Fact]
    public void The_starter_cannot_decline_their_own_call()
    {
        Refused(() => Ringing().Decline(Alice, T0), ChatRuleCodes.CallNotParticipant);
    }

    [Fact]
    public void Cancel_by_the_starter_ends_as_cancelled()
    {
        var call = Ringing();

        call.Cancel(Alice, T0.AddSeconds(2));

        call.State.Should().Be(CallState.Ended);
        call.EndReason.Should().Be(CallEndReason.Cancelled);
    }

    [Fact]
    public void Cancel_by_anyone_else_is_refused()
    {
        Refused(() => Ringing().Cancel(Bob, T0), ChatRuleCodes.CallNotParticipant);
    }

    [Fact]
    public void Miss_ends_a_ringing_call_as_missed()
    {
        var call = Ringing();

        call.Miss(T0.AddSeconds(45));

        call.EndReason.Should().Be(CallEndReason.Missed);
        call.EndedAt.Should().Be(T0.AddSeconds(45));
    }

    [Fact]
    public void End_completes_an_active_direct_call_for_either_person()
    {
        var byAnswerer = Active();
        byAnswerer.End(Bob, T0.AddSeconds(60));
        byAnswerer.EndReason.Should().Be(CallEndReason.Completed);

        var byStarter = Active();
        byStarter.End(Alice, T0.AddSeconds(60));
        byStarter.State.Should().Be(CallState.Ended);
    }

    [Fact]
    public void End_in_a_group_call_is_for_the_starter_only()
    {
        var call = Active(SpaceKind.Group);

        Refused(() => call.End(Bob, T0.AddSeconds(20)), ChatRuleCodes.CallNotParticipant);
        call.End(Alice, T0.AddSeconds(21));

        call.EndReason.Should().Be(CallEndReason.Completed);
    }

    [Fact]
    public void End_of_a_direct_call_by_an_outsider_is_refused()
    {
        Refused(() => Active().End(Carol, T0.AddSeconds(20)), ChatRuleCodes.CallNotParticipant);
    }

    [Fact]
    public void Leave_ends_a_direct_call()
    {
        var call = Active();

        call.Leave(Bob, T0.AddSeconds(30)).Should().BeTrue();

        call.State.Should().Be(CallState.Ended);
        call.EndReason.Should().Be(CallEndReason.Completed);
    }

    [Fact]
    public void Leave_keeps_a_group_call_alive_until_it_is_empty()
    {
        var call = Active(SpaceKind.Group);

        call.Leave(Bob, T0.AddSeconds(30));
        call.State.Should().Be(CallState.Active);
        call.EmptySince.Should().BeNull();

        call.Leave(Alice, T0.AddSeconds(40));
        call.State.Should().Be(CallState.Active);
        call.EmptySince.Should().Be(T0.AddSeconds(40));
    }

    [Fact]
    public void Joining_an_empty_group_call_clears_the_empty_timer()
    {
        var call = Active(SpaceKind.Group);
        call.Leave(Bob, T0.AddSeconds(30));
        call.Leave(Alice, T0.AddSeconds(31));

        call.Join(Carol, T0.AddSeconds(40)).Should().BeTrue();

        call.EmptySince.Should().BeNull();
    }

    [Fact]
    public void Leave_by_a_person_who_is_not_in_the_call_changes_nothing()
    {
        var call = Active(SpaceKind.Group);
        var version = call.Version;

        call.Leave(Carol, T0.AddSeconds(30)).Should().BeFalse();

        call.Version.Should().Be(version);
    }

    [Fact]
    public void EndEmpty_completes_an_empty_active_call_and_refuses_a_populated_one()
    {
        var empty = Active(SpaceKind.Group);
        empty.Leave(Bob, T0.AddSeconds(30));
        empty.Leave(Alice, T0.AddSeconds(31));
        empty.EndEmpty(T0.AddSeconds(95));
        empty.EndReason.Should().Be(CallEndReason.Completed);

        Refused(() => Active(SpaceKind.Group).EndEmpty(T0.AddSeconds(95)), ChatRuleCodes.CallStateInvalid);
    }

    [Fact]
    public void Fail_ends_a_ringing_or_an_active_call_as_failed()
    {
        var ringing = Ringing();
        ringing.Fail(T0.AddSeconds(1));
        ringing.EndReason.Should().Be(CallEndReason.Failed);

        var active = Active();
        active.Fail(T0.AddSeconds(10));
        active.EndReason.Should().Be(CallEndReason.Failed);
    }

    [Fact]
    public void Join_adds_a_member_to_an_active_group_call_and_a_ringing_join_activates_it()
    {
        var active = Active(SpaceKind.Group);
        active.Join(Carol, T0.AddSeconds(10)).Should().BeTrue();
        active.JoinedCount.Should().Be(3);
        active.Join(Carol, T0.AddSeconds(11)).Should().BeFalse();

        var ringing = Ringing(SpaceKind.ChannelPublic);
        ringing.Join(Bob, T0.AddSeconds(2)).Should().BeTrue();
        ringing.State.Should().Be(CallState.Active);
    }

    [Fact]
    public void A_person_who_left_can_join_again_on_the_same_row()
    {
        var call = Active(SpaceKind.Group);
        call.Leave(Bob, T0.AddSeconds(10));

        call.Join(Bob, T0.AddSeconds(20)).Should().BeTrue();

        call.Participants.Count(p => p.PersonId == Bob).Should().Be(1);
        call.IsJoined(Bob).Should().BeTrue();
    }

    [Fact]
    public void A_voice_call_holds_25_people_and_refuses_the_26th()
    {
        var call = Active(SpaceKind.Group, CallKind.Voice);
        for (var i = 0; i < ChatLimits.CallMaxParticipantsVoice - 2; i++) call.Join(Guid.NewGuid(), T0.AddSeconds(10));
        call.JoinedCount.Should().Be(25);

        Refused(() => call.Join(Guid.NewGuid(), T0.AddSeconds(11)), ChatRuleCodes.CallFull);
    }

    [Fact]
    public void A_video_call_holds_12_people_and_refuses_the_13th()
    {
        var call = Active(SpaceKind.ChannelPublic, CallKind.Video);
        for (var i = 0; i < ChatLimits.CallMaxParticipantsVideo - 2; i++) call.Join(Guid.NewGuid(), T0.AddSeconds(10));
        call.JoinedCount.Should().Be(12);

        Refused(() => call.Join(Guid.NewGuid(), T0.AddSeconds(11)), ChatRuleCodes.CallFull);
    }

    [Fact]
    public void A_direct_call_has_exactly_two_people()
    {
        var call = Active();

        Refused(() => call.Join(Carol, T0.AddSeconds(10)), ChatRuleCodes.CallFull);
    }

    [Fact]
    public void Media_state_is_the_callers_own_and_a_repeat_changes_nothing()
    {
        var call = Active(kind: CallKind.Video);

        call.UpdateMediaState(Bob, audioOn: false, videoOn: true, sharingScreen: false, T0.AddSeconds(8)).Should().BeTrue();
        var version = call.Version;
        call.UpdateMediaState(Bob, audioOn: false, videoOn: true, sharingScreen: false, T0.AddSeconds(9)).Should().BeFalse();

        call.Version.Should().Be(version);
        var bob = call.Participants.Single(p => p.PersonId == Bob);
        (bob.AudioOn, bob.VideoOn).Should().Be((false, true));
    }

    [Fact]
    public void A_voice_call_carries_no_camera()
    {
        Refused(() => Active().UpdateMediaState(Bob, true, true, false, T0.AddSeconds(8)), ChatRuleCodes.CallMediaNotAllowed);
    }

    [Fact]
    public void A_voice_call_carries_no_screen_share()
    {
        Refused(() => Active().UpdateMediaState(Bob, true, false, true, T0.AddSeconds(8)), ChatRuleCodes.CallMediaNotAllowed);
    }

    private static Call VideoRoom()
    {
        var call = Ringing(SpaceKind.ChannelPublic, CallKind.Video);
        call.Join(Bob, T0.AddSeconds(5));
        call.Join(Carol, T0.AddSeconds(6));
        return call;
    }

    private static bool Shares(Call call, Guid person) => call.Participants.Single(p => p.PersonId == person).SharingScreen;

    [Fact]
    public void One_person_shares_a_screen_and_stopping_clears_it()
    {
        var call = VideoRoom();

        call.UpdateMediaState(Bob, true, false, true, T0.AddSeconds(8)).Should().BeTrue();
        Shares(call, Bob).Should().BeTrue();
        call.UpdateMediaState(Bob, true, false, false, T0.AddSeconds(9)).Should().BeTrue();
        Shares(call, Bob).Should().BeFalse();
    }

    [Fact]
    public void A_second_share_is_refused_while_someone_shares_and_nothing_changes()
    {
        var call = VideoRoom();
        call.UpdateMediaState(Bob, true, false, true, T0.AddSeconds(8));
        var version = call.Version;

        Refused(() => call.UpdateMediaState(Carol, true, false, true, T0.AddSeconds(9)), ChatRuleCodes.CallScreenShareTaken);

        call.Version.Should().Be(version);
        (Shares(call, Bob), Shares(call, Carol)).Should().Be((true, false));
    }

    [Fact]
    public void A_confirmed_second_share_replaces_the_first_in_one_change()
    {
        var call = VideoRoom();
        call.UpdateMediaState(Bob, true, false, true, T0.AddSeconds(8));
        var version = call.Version;

        call.UpdateMediaState(Carol, true, false, true, T0.AddSeconds(9), replaceShare: true).Should().BeTrue();

        (Shares(call, Bob), Shares(call, Carol)).Should().Be((false, true));
        call.Version.Should().Be(version + 1, "the swap is one change, so every client sees one frame");
    }

    [Fact]
    public void A_person_who_stops_sharing_never_blocks_the_next_share_and_a_repeat_changes_nothing()
    {
        var call = VideoRoom();
        call.UpdateMediaState(Bob, true, false, true, T0.AddSeconds(8));
        call.UpdateMediaState(Bob, true, false, true, T0.AddSeconds(9)).Should().BeFalse("the same values change nothing");

        call.UpdateMediaState(Bob, true, false, false, T0.AddSeconds(10));
        call.UpdateMediaState(Carol, true, false, true, T0.AddSeconds(11)).Should().BeTrue();
        Shares(call, Carol).Should().BeTrue();
    }

    [Fact]
    public void A_replace_with_nobody_sharing_is_an_ordinary_share()
    {
        var call = VideoRoom();

        call.UpdateMediaState(Carol, true, false, true, T0.AddSeconds(8), replaceShare: true).Should().BeTrue();

        Shares(call, Carol).Should().BeTrue();
    }

    [Fact]
    public void A_sharer_who_leaves_stops_sharing_and_the_next_person_may_share()
    {
        var call = VideoRoom();
        call.UpdateMediaState(Bob, true, false, true, T0.AddSeconds(8));

        call.Leave(Bob, T0.AddSeconds(9));

        Shares(call, Bob).Should().BeFalse("a person who left shares nothing");
        call.UpdateMediaState(Carol, true, false, true, T0.AddSeconds(10)).Should().BeTrue();
    }

    [Fact]
    public void A_person_removed_from_the_call_while_sharing_stops_sharing()
    {
        var call = VideoRoom();
        call.UpdateMediaState(Bob, true, false, true, T0.AddSeconds(8));

        call.Revoke(Bob, T0.AddSeconds(9));

        Shares(call, Bob).Should().BeFalse();
    }

    [Fact]
    public void A_call_that_ends_while_someone_shares_clears_every_share()
    {
        var call = Active(kind: CallKind.Video);
        call.UpdateMediaState(Bob, true, true, true, T0.AddSeconds(8));

        call.End(Alice, T0.AddSeconds(9));

        call.Participants.Should().OnlyContain(p => !p.SharingScreen);
    }

    [Fact]
    public void A_rejoin_after_leaving_starts_without_a_share()
    {
        var call = VideoRoom();
        call.UpdateMediaState(Bob, true, false, true, T0.AddSeconds(8));
        call.Leave(Bob, T0.AddSeconds(9));

        call.Join(Bob, T0.AddSeconds(10));

        Shares(call, Bob).Should().BeFalse();
    }

    [Fact]
    public void Media_state_of_a_person_who_is_not_in_the_call_is_refused()
    {
        Refused(() => Active().UpdateMediaState(Carol, true, false, false, T0.AddSeconds(8)), ChatRuleCodes.CallNotParticipant);
    }

    [Fact]
    public void Every_change_bumps_the_version_and_raises_one_fact()
    {
        var call = Ringing();
        call.Answer(Bob, T0.AddSeconds(1));
        call.UpdateMediaState(Bob, false, false, false, T0.AddSeconds(2));
        call.End(Alice, T0.AddSeconds(3));

        call.Version.Should().Be(4);
        call.DomainEvents.OfType<CallChangedDomainEvent>().Select(e => e.Version).Should().Equal(1, 2, 3, 4);
        call.DomainEvents.OfType<CallChangedDomainEvent>().Last().ParticipantIds.Should().BeEmpty();
    }

    [Fact]
    public void Ending_releases_every_participant()
    {
        var call = Active();

        call.End(Alice, T0.AddSeconds(20));

        call.Participants.Should().OnlyContain(p => p.LeftAt == T0.AddSeconds(20));
    }

    public static IEnumerable<object[]> EveryCommandOnAnEndedCall()
    {
        yield return [(Action<Call>)(c => c.Answer(Carol, T0))];
        yield return [(Action<Call>)(c => c.Join(Carol, T0))];
        yield return [(Action<Call>)(c => c.Decline(Bob, T0))];
        yield return [(Action<Call>)(c => c.Cancel(Alice, T0))];
        yield return [(Action<Call>)(c => c.Miss(T0))];
        yield return [(Action<Call>)(c => c.Fail(T0))];
        yield return [(Action<Call>)(c => c.End(Alice, T0))];
        yield return [(Action<Call>)(c => c.Leave(Alice, T0))];
        yield return [(Action<Call>)(c => c.EndEmpty(T0))];
        yield return [(Action<Call>)(c => c.UpdateMediaState(Alice, true, false, false, T0))];
    }

    [Theory]
    [MemberData(nameof(EveryCommandOnAnEndedCall))]
    public void No_transition_leaves_ended(Action<Call> command)
    {
        var call = Ringing();
        call.Cancel(Alice, T0.AddSeconds(1));

        Refused(() => command(call), ChatRuleCodes.CallEnded);
    }

    public static IEnumerable<object[]> IllegalFromRinging()
    {
        yield return [(Action<Call>)(c => c.End(Alice, T0)), ChatRuleCodes.CallStateInvalid];
        yield return [(Action<Call>)(c => c.Leave(Alice, T0)), ChatRuleCodes.CallStateInvalid];
        yield return [(Action<Call>)(c => c.EndEmpty(T0)), ChatRuleCodes.CallStateInvalid];
    }

    [Theory]
    [MemberData(nameof(IllegalFromRinging))]
    public void Some_commands_are_illegal_while_ringing(Action<Call> command, string code)
    {
        Refused(() => command(Ringing()), code);
    }

    public static IEnumerable<object[]> IllegalFromActive()
    {
        yield return [(Action<Call>)(c => c.Decline(Bob, T0)), ChatRuleCodes.CallStateInvalid];
        yield return [(Action<Call>)(c => c.Cancel(Alice, T0)), ChatRuleCodes.CallStateInvalid];
        yield return [(Action<Call>)(c => c.Miss(T0)), ChatRuleCodes.CallStateInvalid];
    }

    [Theory]
    [MemberData(nameof(IllegalFromActive))]
    public void Some_commands_are_illegal_while_active(Action<Call> command, string code)
    {
        Refused(() => command(Active()), code);
    }

    [Fact]
    public void Answer_of_an_active_call_by_a_newcomer_is_refused_and_join_is_the_way_in()
    {
        Refused(() => Active(SpaceKind.Group).Answer(Carol, T0.AddSeconds(9)), ChatRuleCodes.CallStateInvalid);
    }
}
