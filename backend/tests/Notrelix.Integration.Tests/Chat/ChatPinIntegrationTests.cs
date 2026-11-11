using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Chat.Pins.Commands.PinMessage;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-F06 against PostgreSQL. Each pin runs in its own context and transaction, the way the pipeline runs a write: the
/// space row lock must make the count of pins and the insert one step, so racing pins at the limit cannot pass 50, and racing
/// pins of one message store one pin. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatPinIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private const int Racers = 5;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private Guid _spaceId;

    public ChatPinIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "pins", null, null, 1, _member, Now);
        _spaceId = channel.Id;
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ChatSpaces.Add(channel);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private Mock<ICurrentRequestContext> RequestAs(Guid userId)
    {
        var context = new Mock<ICurrentRequestContext>();
        context.Setup(c => c.UserId).Returns(userId);
        context.Setup(c => c.IsAuthenticated).Returns(true);
        context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        return context;
    }

    private Mock<IWorkspaceMemberFacts> ActiveMembers(Guid userId)
    {
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, "Member", true));
        return members;
    }

    /// <summary>Seeds messages (each takes the next space sequence) and, for the first <paramref name="pinned"/> of them, a pin.</summary>
    private async Task<List<Guid>> SeedMessagesAsync(int count, int pinned)
    {
        var ids = new List<Guid>();
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        var allocator = new ChatSpaceSequenceAllocator(seed);
        for (var i = 0; i < count; i++)
        {
            var sequence = await allocator.NextAsync(_spaceId, CancellationToken.None);
            var message = Message.CreateUserMessage(_accountId, _workspaceId, _spaceId, sequence, _member, $"message {i}", $"seed-{Guid.NewGuid():N}", null, Now);
            seed.ChatMessages.Add(message);
            ids.Add(message.Id);
            if (i < pinned)
                seed.ChatPins.Add(Pin.Create(_accountId, _workspaceId, _spaceId, message.Id, _member, Now));
        }
        await seed.SaveChangesAsync();
        return ids;
    }

    /// <summary>One pin, in its own transaction. A refused pin rolls back and reports the rule code.</summary>
    private async Task<(bool Created, string? RuleCode)> PinInOwnTransactionAsync(Guid messageId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var allocator = new ChatSpaceSequenceAllocator(context);
        var clock = new FixedClock(Now);
        var handler = new PinMessageCommandHandler(
            context,
            RequestAs(_member).Object,
            ActiveMembers(_member).Object,
            allocator,
            new ChatSystemMessages(context, allocator, clock),
            clock);

        try
        {
            var result = await handler.Handle(new PinMessageCommand(_workspaceId, messageId), CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (result.Data!.Created, null);
        }
        catch (Notrelix.Domain.Common.Exceptions.BusinessRuleException refused)
        {
            await transaction.RollbackAsync();
            return (false, refused.RuleCode);
        }
    }

    [Fact]
    public async Task RacingPinsAtTheLimit_NeverPassFiftyPins()
    {
        // Forty-nine pins are stored; five racers each pin a different message for the last slot.
        var ids = await SeedMessagesAsync(49 + Racers, pinned: 49);
        var racing = ids.Skip(49).ToList();

        var outcomes = await Task.WhenAll(racing.Select(PinInOwnTransactionAsync));

        outcomes.Count(o => o.Created).Should().Be(1, "exactly one racer takes the last slot under the space lock");
        outcomes.Count(o => o.RuleCode == ChatRuleCodes.LimitReached).Should().Be(Racers - 1, "the others are refused with limit-reached");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ChatPins.IgnoreQueryFilters().CountAsync(p => p.SpaceId == _spaceId)).Should().Be(ChatLimits.PinLimit);
    }

    [Fact]
    public async Task RacingPinsOfOneMessage_StoreOnePin_AndOneAnnouncement()
    {
        var ids = await SeedMessagesAsync(1, pinned: 0);

        var outcomes = await Task.WhenAll(Enumerable.Range(0, Racers).Select(_ => PinInOwnTransactionAsync(ids[0])));

        outcomes.Count(o => o.Created).Should().Be(1, "exactly one racer creates the pin; the others get it back");
        outcomes.Should().OnlyContain(o => o.RuleCode == null);
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ChatPins.IgnoreQueryFilters().CountAsync(p => p.MessageId == ids[0])).Should().Be(1);
        (await probe.ChatMessages.IgnoreQueryFilters().CountAsync(m => m.SpaceId == _spaceId && m.Kind == MessageKind.System)).Should().Be(1,
            "the announcement is stored once, with the pin");
    }
}
