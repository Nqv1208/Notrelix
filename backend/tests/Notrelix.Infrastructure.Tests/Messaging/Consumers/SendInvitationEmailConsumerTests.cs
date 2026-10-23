using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Email;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Infrastructure.Messaging.Consumers.Workspaces.InvitationDelivery;
using Notrelix.Infrastructure.Notifications.Email;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>WSP-026: a resource invitation is delivered with its own template; a workspace invitation keeps the original one.</summary>
public class SendInvitationEmailConsumerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 0, 0, 0, TimeSpan.Zero);

    private static WorkspaceInvitationDeliveryRequestedIntegrationEventV1 Event(
        string? resourceKind = null, string? resourceName = null, string? inviterName = null) => new(
        EventId: Guid.CreateVersion7(),
        InvitationId: Guid.CreateVersion7(),
        AccountId: Guid.CreateVersion7(),
        WorkspaceId: Guid.CreateVersion7(),
        RecipientEmail: "guest@example.com",
        ProtectedToken: "protected",
        HashVersion: 1,
        TokenGeneration: 2,
        ExpiresAt: Now.AddDays(7),
        InvitedBy: Guid.CreateVersion7(),
        CorrelationId: Guid.CreateVersion7(),
        OccurredAt: Now,
        ResourceKind: resourceKind,
        ResourceId: resourceKind is null ? null : Guid.CreateVersion7(),
        ResourceName: resourceName,
        InviterName: inviterName);

    private static async Task<(string TemplateKey, string DeduplicationKey, object Payload)> Run(
        WorkspaceInvitationDeliveryRequestedIntegrationEventV1 message)
    {
        var writer = new Mock<IEmailOutboxWriter>();
        string? templateKey = null;
        string? dedup = null;
        object? payload = null;
        writer.Setup(w => w.QueueTemplatedEmailAsync(
                It.IsAny<QueueTemplatedEmailRequest<WorkspaceInvitationEmailPayload>>(), It.IsAny<CancellationToken>()))
            .Callback<QueueTemplatedEmailRequest<WorkspaceInvitationEmailPayload>, CancellationToken>((r, _) =>
            { templateKey = r.TemplateKey; dedup = r.DeduplicationKey; payload = r.Payload; })
            .Returns(Task.CompletedTask);
        writer.Setup(w => w.QueueTemplatedEmailAsync(
                It.IsAny<QueueTemplatedEmailRequest<ResourceInvitationEmailPayload>>(), It.IsAny<CancellationToken>()))
            .Callback<QueueTemplatedEmailRequest<ResourceInvitationEmailPayload>, CancellationToken>((r, _) =>
            { templateKey = r.TemplateKey; dedup = r.DeduplicationKey; payload = r.Payload; })
            .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<WorkspaceInvitationDeliveryRequestedIntegrationEventV1>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await new SendInvitationEmailConsumer(writer.Object, NullLogger<SendInvitationEmailConsumer>.Instance)
            .Consume(context.Object);

        return (templateKey!, dedup!, payload!);
    }

    [Fact]
    public async Task WorkspaceInvitation_UsesTheWorkspaceTemplate()
    {
        var message = Event();

        var (key, dedup, payload) = await Run(message);

        key.Should().Be("workspace-invitation");
        dedup.Should().Be($"workspace-invitation:{message.InvitationId}:generation:2");
        payload.Should().BeOfType<WorkspaceInvitationEmailPayload>();
    }

    [Fact]
    public async Task ResourceInvitation_UsesTheResourceTemplateWithTheResourceAndInviter()
    {
        var message = Event("work-management.board", "Triển khai ERP Q4", "Mai Linh");

        var (key, dedup, payload) = await Run(message);

        key.Should().Be("resource-invitation");
        dedup.Should().Be($"resource-invitation:{message.InvitationId}:generation:2");
        var resource = payload.Should().BeOfType<ResourceInvitationEmailPayload>().Subject;
        resource.ResourceKind.Should().Be("work-management.board");
        resource.ResourceName.Should().Be("Triển khai ERP Q4");
        resource.InviterName.Should().Be("Mai Linh");
    }

    [Fact]
    public void ResourceTemplate_NamesTheBoardAndTheInviter_NotAWorkspace()
    {
        var payload = new ResourceInvitationEmailPayload(
            Guid.NewGuid(), 1, new ProtectedSecretEnvelope("p"), Now, "work-management.board", "Q4 <ERP>", "Mai Linh");

        var email = ResourceInvitationEmailMaterializer.RenderContent(payload, "guest@example.com", "https://x/invite?t=1&u=2", "October 30, 2026");

        email.Subject.Should().Be("Mai Linh invited you to the board \"Q4 <ERP>\"");
        email.BodyHtml.Should().Contain("<strong>Mai Linh</strong>").And.Contain("board <strong>Q4 &lt;ERP&gt;</strong>");
        email.BodyHtml.Should().Contain("href=\"https://x/invite?t=1&amp;u=2\"").And.NotContain("workspace");
        email.BodyText.Should().Contain("Mai Linh invited you to the board").And.NotContain("workspace");
    }

    [Fact]
    public void ResourceTemplate_NamesAPage()
    {
        var payload = new ResourceInvitationEmailPayload(
            Guid.NewGuid(), 1, new ProtectedSecretEnvelope("p"), Now, "documents.page", "Runbook", "");

        var email = ResourceInvitationEmailMaterializer.RenderContent(payload, "guest@example.com", "https://x", "October 30, 2026");

        email.Subject.Should().Be("Someone invited you to the page \"Runbook\"");
    }
}
