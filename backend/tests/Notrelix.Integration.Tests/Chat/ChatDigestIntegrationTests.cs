using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Notifications.Services;
using Notrelix.Application.Features.Chat.Public.Digest;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Features.Identity.Public.DeliveryAddresses;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Configuration;
using Notrelix.Infrastructure.CrossContext.Collaboration.Notifications;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Identity.Users;
using Notrelix.Infrastructure.Notifications.ChatDigest;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-H09 against PostgreSQL: three missed mentions become ONE queued digest email for the person who turned the digest on, a repeated
/// pass adds nothing (the unique deduplication key), a Guest and a person who read the messages get none, and the email carries the address
/// the system-sender fact finds. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatDigestIntegrationTests
{
    private static readonly DateTimeOffset Window0 = new(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Sent = new(2026, 11, 13, 1, 0, 0, TimeSpan.Zero);

    private readonly OrgTreeTestHost _host;

    public ChatDigestIntegrationTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db) { Now = Sent };

    private ServiceProvider ProviderFor(OrgStack stack) =>
        _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
        {
            services.AddSingleton<IOptions<ChatDigestOptions>>(Options.Create(new ChatDigestOptions { MaxItems = 2, BatchSize = 1 }));
            services.AddSingleton<IOptions<FrontendOptions>>(Options.Create(new FrontendOptions { AppBaseUrl = new Uri("https://app.test/") }));
            services.AddScoped<IChatDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IChatDigestFacts, ChatDigestFacts>();
            services.AddScoped<IMemberDeliveryAddresses, MemberDeliveryAddresses>();
            services.AddScoped<ChatDigestComposer>();
            services.AddScoped<ChatDigestRunner>();
        });

    private static async Task RunAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ChatDigestRunner>().RunOnceAsync(CancellationToken.None);
    }

    private async Task<(Space Channel, List<Message> Messages)> SeedChatAsync(OrgStack stack, params Guid[] members)
    {
        var channel = Space.CreateChannel(stack.AccountId, stack.WorkspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, stack.OwnerId, Window0);
        foreach (var member in members)
            channel.AddMember(member, MembershipRole.Member, Window0);
        var messages = Enumerable.Range(1, 3)
            .Select(i => Message.CreateUserMessage(stack.AccountId, stack.WorkspaceId, channel.Id, i, stack.OwnerId, $"please look at item {i}", $"c-{i}", null, Window0.AddHours(8 + i)))
            .ToList();
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatSpaces.Add(channel);
            seed.ChatMessages.AddRange(messages);
            return Task.CompletedTask;
        });
        return (channel, messages);
    }

    private async Task NotifyAsync(OrgStack stack, Space channel, IEnumerable<Message> messages, params Guid[] people)
    {
        foreach (var message in messages)
        {
            await _host.WithSystemContextAsync(async seed =>
            {
                var source = new ChatNotificationSource(
                    stack.AccountId, stack.WorkspaceId, channel.Id, message.Id, null, stack.OwnerId, Guid.NewGuid(), "general", message.Content, message.CreatedAt);
                await new PostgresChatNotificationIntents(seed, NullLogger<PostgresChatNotificationIntents>.Instance)
                    .DeliverAsync(source, people.Select(p => new ChatNotificationIntent(p, ChatNotificationReasons.Mention)).ToList(), CancellationToken.None);
            });
        }
    }

    private Task DigestOnAsync(OrgStack stack, params Guid[] people) =>
        _host.WithSystemContextAsync(seed =>
        {
            foreach (var person in people)
            {
                var set = NotificationPreferenceSet.Create(
                    stack.AccountId, stack.WorkspaceId, person, [new NotificationPreferenceChange(NotificationTypes.Digest, NotificationChannels.Email, true)], Window0)!;
                seed.NotificationPreferenceSets.Add(set);
            }

            return Task.CompletedTask;
        });

    private Task<List<Notrelix.Infrastructure.Data.Notifications.EmailOutboxMessage>> MailsAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.EmailOutboxMessages.AsNoTracking().Where(m => m.WorkspaceId == stack.WorkspaceId).OrderBy(m => m.RecipientEmail).ToListAsync());

    [Fact]
    public async Task ThreeMissedMentionsAreOneQueuedEmail_ARepeatedPassAddsNothing_AGuestAndAPersonWithoutThePreferenceGetNone()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var ana = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Ana");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var bao = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Bao");
        var (channel, messages) = await SeedChatAsync(stack, ana, guest, bao);
        await NotifyAsync(stack, channel, messages, ana, guest, bao);
        await DigestOnAsync(stack, ana, guest);

        using var provider = ProviderFor(stack);
        await RunAsync(provider);
        await RunAsync(provider);

        var anaEmail = await _host.ReadAsync(c => c.Users.Where(u => u.Id == ana).Select(u => u.NormalizedEmail).SingleAsync());
        var mails = await MailsAsync(stack);
        var mail = mails.Should().ContainSingle("Bao did not turn the digest on, the Guest has no address and the second pass adds nothing").Subject;
        mail.RecipientEmail.Should().Be(anaEmail);
        mail.SourceContext.Should().Be("chat");
        mail.Subject.Should().Be("You missed 3 chat messages");
        mail.BodyText.Should().Contain("and 1 more", "the cap of this run is 2 lines");
        mail.BodyText.Should().Contain($"https://app.test/workspaces/{stack.WorkspaceId}/chat/{channel.Id}");
    }

    [Fact]
    public async Task MessagesThePersonReadInChatAreNotMissed_AndNothingIsQueuedWhenNothingIsLeft()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var ana = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Ana");
        var (channel, messages) = await SeedChatAsync(stack, ana);
        await NotifyAsync(stack, channel, messages, ana);
        await DigestOnAsync(stack, ana);
        await _host.WithSystemContextAsync(async c =>
        {
            var space = await c.ChatSpaces.Include(s => s.Memberships).SingleAsync(s => s.Id == channel.Id);
            space.Memberships.Single(m => m.UserId == ana).AdvanceReadSequence(3);
        });
        (await _host.ReadAsync(c => c.ChatMemberships.Where(m => m.UserId == ana).Select(m => m.ReadSequence).SingleAsync())).Should().Be(3);

        using var provider = ProviderFor(stack);
        await RunAsync(provider);

        (await MailsAsync(stack)).Should().BeEmpty();
    }

    [Fact]
    public async Task APersonRemovedFromTheChannelBeforeTheSendGetsNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var ana = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Ana");
        var (channel, messages) = await SeedChatAsync(stack, ana);
        await NotifyAsync(stack, channel, messages, ana);
        await DigestOnAsync(stack, ana);
        await _host.WithSystemContextAsync(async c =>
        {
            var space = await c.ChatSpaces.Include(s => s.Memberships).SingleAsync(s => s.Id == channel.Id);
            space.RemoveMember(ana, Sent.AddMinutes(-10));
        });
        (await _host.ReadAsync(c => c.ChatMemberships.Where(m => m.UserId == ana).Select(m => m.LeftAt).SingleAsync())).Should().NotBeNull();

        using var provider = ProviderFor(stack);
        await RunAsync(provider);

        (await MailsAsync(stack)).Should().BeEmpty();
    }
}
