using System.Collections.Concurrent;
using Moq;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Time;
using Notrelix.Application.Features.Chat.ReadState.Commands.SetNotificationLevel;
using Notrelix.Application.Features.Chat.ReadState.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-D01, D02, D05, D07 and CHT-009 against PostgreSQL: the grouped unread count is right across several spaces and
/// authors, the read boundary only moves forward under concurrent requests, a level change does not undo a boundary that
/// moved meanwhile, and a private space's read state is not visible to a person who is not in it. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatReadStateIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _otherWorkspaceId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private Guid _alpha;
    private Guid _beta;
    private Guid _gamma;
    private Guid _secret;

    public ChatReadStateIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        // alpha (public): owner, member, other. Messages by sequence: 1 owner, 2 member, 3 other, 4 owner system,
        // 5 other (deleted later), 6 owner. Last sequence 6.
        var alpha = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "alpha", null, null, 1, _owner, Now);
        alpha.AddMember(_member, MembershipRole.Member, Now);
        alpha.AddMember(_other, MembershipRole.Member, Now);
        _alpha = alpha.Id;
        var alphaMessages = new List<Message>
        {
            UserMessage(_alpha, _workspaceId, 1, _owner, "a1"),
            UserMessage(_alpha, _workspaceId, 2, _member, "a2"),
            UserMessage(_alpha, _workspaceId, 3, _other, "a3"),
            Message.CreateSystemMessage(_accountId, _workspaceId, _alpha, 4, _owner, "joined", Now),
            UserMessage(_alpha, _workspaceId, 5, _other, "a5"),
            UserMessage(_alpha, _workspaceId, 6, _owner, "a6"),
        };
        alphaMessages[4].Delete(_other, Now);

        // beta (public): owner, member, other. Messages 1 to 4 by owner and other. Last sequence 4.
        var beta = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "beta", null, null, 2, _owner, Now);
        beta.AddMember(_member, MembershipRole.Member, Now);
        beta.AddMember(_other, MembershipRole.Member, Now);
        _beta = beta.Id;
        var betaMessages = new List<Message>
        {
            UserMessage(_beta, _workspaceId, 1, _owner, "b1"),
            UserMessage(_beta, _workspaceId, 2, _other, "b2"),
            UserMessage(_beta, _workspaceId, 3, _owner, "b3"),
            UserMessage(_beta, _workspaceId, 4, _other, "b4"),
        };

        // gamma (private): owner and member. Messages 1 and 2 by owner. Last sequence 2.
        var gamma = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPrivate, "gamma", null, null, 3, _owner, Now);
        gamma.AddMember(_member, MembershipRole.Member, Now);
        _gamma = gamma.Id;
        var gammaMessages = new List<Message>
        {
            UserMessage(_gamma, _workspaceId, 1, _owner, "g1"),
            UserMessage(_gamma, _workspaceId, 2, _owner, "g2"),
        };

        // secret (private): the owner only. The outsider is in no membership of it.
        var secret = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPrivate, "secret", null, null, 4, _owner, Now);
        _secret = secret.Id;
        var secretMessages = new List<Message> { UserMessage(_secret, _workspaceId, 1, _owner, "s1") };

        // A channel of another Workspace in which the member is also a member: it must not appear in this Workspace.
        var foreign = Space.CreateChannel(_accountId, _otherWorkspaceId, SpaceKind.ChannelPublic, "foreign", null, null, 1, _owner, Now);
        foreign.AddMember(_member, MembershipRole.Member, Now);
        var foreignMessages = new List<Message> { UserMessage(foreign.Id, _otherWorkspaceId, 1, _owner, "f1") };

        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.ChatSpaces.AddRange(alpha, beta, gamma, secret, foreign);
            seed.ChatMessages.AddRange(alphaMessages.Concat(betaMessages).Concat(gammaMessages).Concat(secretMessages).Concat(foreignMessages));
            await seed.SaveChangesAsync();
        }

        await SetLastSequenceAsync(alpha.Id, 6);
        await SetLastSequenceAsync(beta.Id, 4);
        await SetLastSequenceAsync(gamma.Id, 2);
        await SetLastSequenceAsync(secret.Id, 1);
        await SetLastSequenceAsync(foreign.Id, 1);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private Message UserMessage(Guid spaceId, Guid workspaceId, long sequence, Guid author, string text) =>
        Message.CreateUserMessage(_accountId, workspaceId, spaceId, sequence, author, text, $"{spaceId:N}-{sequence}", null, Now);

    private async Task SetLastSequenceAsync(Guid spaceId, long last)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE chat.spaces SET last_sequence = {last} WHERE id = {spaceId}");
    }

    private async Task<long> ReadSequenceAsync(Guid spaceId, Guid userId)
    {
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        return await probe.ChatMemberships.AsNoTracking()
            .Where(m => m.SpaceId == spaceId && m.UserId == userId)
            .Select(m => m.ReadSequence)
            .SingleAsync();
    }

    /// <summary>One read-boundary move, in its own transaction, the way the pipeline runs a write request.</summary>
    private async Task AdvanceInOwnTransactionAsync(Guid spaceId, Guid userId, long target)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        await new ChatReadStateStore(context).AdvanceAsync(_workspaceId, spaceId, userId, target, CancellationToken.None);
        await transaction.CommitAsync();
    }

    private static Mock<ICurrentRequestContext> RequestAs(Guid userId)
    {
        var context = new Mock<ICurrentRequestContext>();
        context.Setup(c => c.UserId).Returns(userId);
        context.Setup(c => c.IsAuthenticated).Returns(true);
        return context;
    }

    private Mock<IWorkspaceMemberFacts> ActiveMember(Guid userId)
    {
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, "Member", true));
        return members;
    }

    private static Mock<IDateTimeProvider> Clock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        return clock;
    }

    [Fact]
    public async Task GroupedUnreadCount_IsRightAcrossSpacesAndAuthors_AndExcludesOwnSystemAndDeletedMessages()
    {
        // The member read alpha up to 2; beta is unread; gamma is read to its end.
        await AdvanceInOwnTransactionAsync(_alpha, _member, 2);
        await AdvanceInOwnTransactionAsync(_gamma, _member, 2);

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var summary = await UnreadSummaryBuilder.BuildAsync(probe, _workspaceId, _member, Now, CancellationToken.None);

        var alpha = summary.Spaces.Single(s => s.SpaceId == _alpha);
        alpha.UnreadCount.Should().Be(2, "seq 3 (other) and seq 6 (owner) are after the boundary of 2; seq 4 is system, seq 5 is deleted");
        alpha.FirstUnreadSequence.Should().Be(3);
        alpha.ReadSequence.Should().Be(2);
        alpha.LastSequence.Should().Be(6);

        var beta = summary.Spaces.Single(s => s.SpaceId == _beta);
        beta.UnreadCount.Should().Be(4, "nothing was read; the four messages are other people's");
        beta.FirstUnreadSequence.Should().Be(1);

        var gamma = summary.Spaces.Single(s => s.SpaceId == _gamma);
        gamma.UnreadCount.Should().Be(0);
        gamma.FirstUnreadSequence.Should().BeNull();

        summary.Spaces.Should().NotContain(s => s.SpaceId == _secret, "the member is not in the private space");
        summary.Spaces.Should().HaveCount(3, "the channel of another Workspace is not in this summary");
        summary.TotalUnread.Should().Be(6, "2 from alpha plus 4 from beta; gamma has none");
    }

    [Fact]
    public async Task MutedSpace_KeepsItsCount_ButLeavesTheTotal()
    {
        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            var handler = new SetNotificationLevelCommandHandler(context, RequestAs(_member).Object, ActiveMember(_member).Object, Clock().Object);
            var result = await handler.Handle(
                new SetNotificationLevelCommand(_workspaceId, _beta, "none", Now.AddHours(1)),
                CancellationToken.None);
            await context.SaveChangesAsync();

            result.Data!.Muted.Should().BeTrue();
        }

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var summary = await UnreadSummaryBuilder.BuildAsync(probe, _workspaceId, _member, Now, CancellationToken.None);
        var beta = summary.Spaces.Single(s => s.SpaceId == _beta);
        beta.UnreadCount.Should().Be(4);
        beta.Muted.Should().BeTrue();
        summary.TotalUnread.Should().Be(5, "beta is muted and left out; alpha has 3 unread (seq 1, 3, 6) and gamma 2 (seq 1, 2) with no boundary yet");
    }

    [Fact]
    public async Task ConcurrentMarkReads_NeverMoveTheBoundaryBackwards_AndEndAtTheHighestTarget()
    {
        // Thirty requests on one boundary at once, each in its own transaction, in a shuffled order. Targets above the
        // last sequence of beta (4) are clamped by the handler; the store receives them as they are, so the SQL bound is
        // tested with the clamped values the handler sends.
        var targets = Enumerable.Range(1, 30).OrderBy(i => (i * 17) % 31).Select(i => (long)Math.Min(i, 4)).ToArray();
        var observed = new ConcurrentBag<long>();

        await Task.WhenAll(targets.Select(async target =>
        {
            await AdvanceInOwnTransactionAsync(_beta, _member, target);
            observed.Add(target);
        }));

        observed.Should().HaveCount(30);
        (await ReadSequenceAsync(_beta, _member)).Should().Be(4, "the highest target is 4");
    }

    [Fact]
    public async Task ALowerMarkRead_AfterAHigherOne_LeavesTheBoundaryWhereItWas()
    {
        await AdvanceInOwnTransactionAsync(_beta, _member, 3);
        await AdvanceInOwnTransactionAsync(_beta, _member, 1);

        (await ReadSequenceAsync(_beta, _member)).Should().Be(3);
    }

    [Fact]
    public async Task MarkAllRead_TouchesOnlyTheCallersLiveMembershipsInTheWorkspace()
    {
        long touched;
        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            touched = await new ChatReadStateStore(context).AdvanceAllAsync(_workspaceId, _member, CancellationToken.None);
        }

        touched.Should().Be(3, "alpha, beta and gamma; the private secret space is not the caller's and the other Workspace is out of scope");
        (await ReadSequenceAsync(_alpha, _member)).Should().Be(6);
        (await ReadSequenceAsync(_beta, _member)).Should().Be(4);
        (await ReadSequenceAsync(_gamma, _member)).Should().Be(2);
    }

    [Fact]
    public async Task MarkAllRead_LeavesAMessageSentAfterTheCallUnread()
    {
        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            await new ChatReadStateStore(context).AdvanceAllAsync(_workspaceId, _member, CancellationToken.None);
        }

        // A send after the call: the space's sequence goes to 7 and the message is stored with it.
        await using (var send = _db.CreateOwnerContext(SystemTenant()))
        {
            await send.Database.ExecuteSqlInterpolatedAsync($"UPDATE chat.spaces SET last_sequence = 7 WHERE id = {_alpha}");
            send.ChatMessages.Add(UserMessage(_alpha, _workspaceId, 7, _owner, "after the call"));
            await send.SaveChangesAsync();
        }

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var summary = await UnreadSummaryBuilder.BuildAsync(probe, _workspaceId, _member, Now, CancellationToken.None);
        var alpha = summary.Spaces.Single(s => s.SpaceId == _alpha);
        alpha.UnreadCount.Should().Be(1);
        alpha.FirstUnreadSequence.Should().Be(7);
    }

    [Fact]
    public async Task ALevelChange_DoesNotUndoAReadBoundaryThatMovedMeanwhile()
    {
        // The handler loads the membership first (tracked), a mark-read commits from another transaction, then the level is saved.
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var handler = new SetNotificationLevelCommandHandler(context, RequestAs(_member).Object, ActiveMember(_member).Object, Clock().Object);
        var preload = await handler.Handle(
            new SetNotificationLevelCommand(_workspaceId, _beta, "all", null), CancellationToken.None);
        preload.Succeeded.Should().BeTrue();

        await AdvanceInOwnTransactionAsync(_beta, _member, 3);

        var result = await handler.Handle(
            new SetNotificationLevelCommand(_workspaceId, _beta, "mentions", null), CancellationToken.None);
        await context.SaveChangesAsync();

        result.Data!.NotificationLevel.Should().Be("mentions");
        (await ReadSequenceAsync(_beta, _member)).Should().Be(3, "the level is saved without rewriting the boundary");
    }

    [Fact]
    public async Task APrivateSpace_IsInvisibleToAPersonNotInIt()
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());

        var summary = await UnreadSummaryBuilder.BuildAsync(context, _workspaceId, _outsider, Now, CancellationToken.None);
        var moved = await new ChatReadStateStore(context).AdvanceAsync(_workspaceId, _secret, _outsider, 1, CancellationToken.None);

        summary.Spaces.Should().BeEmpty();
        moved.Should().BeFalse("a person not in the space has no membership row to move");
    }
}
