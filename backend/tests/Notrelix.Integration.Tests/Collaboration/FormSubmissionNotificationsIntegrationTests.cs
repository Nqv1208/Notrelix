using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// UC-WM-R07 (D78.2) "new submission on a form I manage": a response tells each person who manages the form (its
/// creator and the Board's Owner and Admin members) once, only while they can still see the Board, never the person
/// who answered, honours the member's own preference, and a redelivery adds nothing. Runs on PostgreSQL.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class FormSubmissionNotificationsIntegrationTests
{
    private const string BoardKind = "work-management.board";
    private readonly OrgTreeTestHost _host;

    public FormSubmissionNotificationsIntegrationTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db);

    private ServiceProvider ProviderFor(OrgStack stack) =>
        _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
        {
            services.AddScoped<Notrelix.Infrastructure.Data.Abstractions.INotificationDbContext>(
                sp => sp.GetRequiredService<Notrelix.Infrastructure.Data.ApplicationDbContext>());
            services.AddScoped<Notrelix.Application.Features.WorkManagement.Public.FormNotifications.IFormNotificationFacts,
                Notrelix.Infrastructure.CrossContext.Collaboration.WorkManagement.FormNotificationFactsAdapter>();
            services.AddScoped<FormSubmissionNotificationConsumer>();
        });

    private static async Task ConsumeAsync(ServiceProvider provider, FormSubmissionCreatedIntegrationEvent message)
    {
        using var scope = provider.CreateScope();
        // The consume pipeline restores the tenant from the event envelope; the test does the same.
        ((Notrelix.Testing.Application.Fakes.FakeCurrentTenantContext)scope.ServiceProvider
                .GetRequiredService<Notrelix.Application.Common.Context.ICurrentTenantContext>())
            .SetWorkspace(message.AccountIdValue, message.WorkspaceIdValue, null);
        // The consume pipeline runs the consumer in the data-session transaction; visibility reads on it.
        var db = scope.ServiceProvider.GetRequiredService<Notrelix.Infrastructure.Data.ApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var context = new Mock<ConsumeContext<FormSubmissionCreatedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<FormSubmissionNotificationConsumer>().Consume(context.Object);
        await transaction.CommitAsync();
    }

    private async Task<(Guid BoardId, Guid FormId)> SeedFormAsync(OrgStack stack, Guid creator, params (Guid User, BoardRole Role)[] members)
    {
        var boardId = await _host.SeedBoardAsync(stack, "Hiring", BoardVisibility.Private);
        foreach (var (user, role) in members)
        {
            await _host.SeedBoardMemberAsync(boardId, user, role);
            await _host.SeedResourcePermissionAsync(stack, BoardKind, boardId, PermissionSubjectType.User, user, PermissionLevel.Manager);
        }

        var form = Form.Create(stack.AccountId, stack.WorkspaceId, boardId, "Candidate intake", $"intake-{Guid.NewGuid():N}",
            creator, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.Forms.Add(form);
            return Task.CompletedTask;
        });
        return (boardId, form.Id);
    }

    private static FormSubmissionCreatedIntegrationEvent Submitted(OrgStack stack, Guid boardId, Guid formId, Guid? submitter) =>
        new(Guid.CreateVersion7(), stack.AccountId, stack.WorkspaceId, Guid.CreateVersion7(), formId, boardId, submitter, Guid.CreateVersion7());

    [Fact]
    public async Task Response_TellsTheManagersWhoSeeTheBoard_OnceEach_NeverTheRespondent_OrACreatorWhoLostAccess()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var ana = await _host.AddMemberAsync(stack, Domain.Workspaces.Members.WorkspaceRole.Member, "Ana");
        var cat = await _host.AddMemberAsync(stack, Domain.Workspaces.Members.WorkspaceRole.Member, "Cat");
        var ben = await _host.AddMemberAsync(stack, Domain.Workspaces.Members.WorkspaceRole.Member, "Ben");
        var dan = await _host.AddMemberAsync(stack, Domain.Workspaces.Members.WorkspaceRole.Member, "Dan");
        // Ben made the form but no longer sees the Private Board; Dan is only a workspace member.
        var (boardId, formId) = await SeedFormAsync(stack, ben, (ana, BoardRole.Admin), (cat, BoardRole.Admin));

        using var provider = ProviderFor(stack);
        var message = Submitted(stack, boardId, formId, cat);
        await ConsumeAsync(provider, message);
        await ConsumeAsync(provider, message);

        var rows = await _host.ReadAsync(c => (
                from r in c.NotificationRecipients
                join n in c.NotificationItems on r.NotificationId equals n.Id
                where n.WorkspaceId == stack.WorkspaceId
                select new { n.NotificationType, n.ResourceKind, n.ResourceId, n.Body, n.ActorUserId, r.RecipientUserId })
            .ToListAsync());

        rows.Should().ContainSingle("Ana manages and sees the Board; Cat answered it herself; Ben lost access; Dan does not manage it");
        var row = rows.Single();
        row.RecipientUserId.Should().Be(ana);
        row.NotificationType.Should().Be(NotificationTypes.FormSubmissionCreatedProducedType);
        row.ResourceKind.Should().Be(BoardKind);
        row.ResourceId.Should().Be(boardId);
        row.Body.Should().Be("Candidate intake", "the notification names the form, never an answer");
        row.ActorUserId.Should().Be(cat);
        _ = dan;
    }

    [Fact]
    public async Task Response_SkipsAManagerWhoTurnedTheAssignedPreferenceOff_AndAFormThatIsGoneTellsNobody()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var ana = await _host.AddMemberAsync(stack, Domain.Workspaces.Members.WorkspaceRole.Member, "Ana");
        var (boardId, formId) = await SeedFormAsync(stack, stack.OwnerId, (ana, BoardRole.Admin));
        await _host.WithSystemContextAsync(seed =>
        {
            seed.NotificationPreferenceSets.Add(NotificationPreferenceSet.Create(
                stack.AccountId, stack.WorkspaceId, ana,
                [new NotificationPreferenceChange(NotificationTypes.Assigned, NotificationChannels.InApp, false)],
                OrgTreeTestHost.BaseTime)!);
            return Task.CompletedTask;
        });

        using var provider = ProviderFor(stack);
        await ConsumeAsync(provider, Submitted(stack, boardId, formId, null));
        await ConsumeAsync(provider, Submitted(stack, boardId, Guid.CreateVersion7(), null));

        var told = await _host.ReadAsync(c => c.NotificationRecipients
            .Where(r => r.WorkspaceId == stack.WorkspaceId).Select(r => r.RecipientUserId).ToListAsync());
        told.Should().BeEquivalentTo([stack.OwnerId], "the creator is told; Ana turned the type off; an unknown form tells nobody");
    }
}
