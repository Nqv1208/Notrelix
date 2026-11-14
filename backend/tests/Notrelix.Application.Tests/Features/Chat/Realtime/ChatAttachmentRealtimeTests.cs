using System.Text;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;

namespace Notrelix.Application.Tests.Features.Chat.Realtime;

/// <summary>
/// UC-CHT-G08 at the level of the realtime fact: a file added to or removed from a message is a compact frame on the channel stream of
/// its space. It carries metadata and the authorized download route, never bytes; an oversized name drops the details.
/// </summary>
public class ChatAttachmentRealtimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid SpaceId = Guid.NewGuid();

    private static ChatMessageAttachmentChangedIntegrationEvent Added(string fileName = "report.pdf") => new(
        EventId: Guid.NewGuid(), AccountId: Guid.NewGuid(), WorkspaceId: Guid.NewGuid(), SpaceId: SpaceId, MessageId: Guid.NewGuid(),
        ChangeKind: "added", AttachmentId: Guid.NewGuid(), FileName: fileName, SizeBytes: 1234, ContentType: "application/pdf",
        UploadedBy: Guid.NewGuid(), AttachmentCreatedAt: Now, CorrelationId: Guid.NewGuid(), ActorUserId: Guid.NewGuid(), OccurredAt: Now);

    private static ChatMessageAttachmentChangedIntegrationEvent Removed() => Added() with
    {
        ChangeKind = "removed",
        FileName = null,
        SizeBytes = null,
        ContentType = null,
        UploadedBy = null,
        AttachmentCreatedAt = null,
    };

    [Fact]
    public void An_added_file_is_a_channel_frame_with_its_metadata_and_download_route()
    {
        var fact = Added();

        var change = ChatRealtime.ForAttachmentChanged(fact, Now);

        change.TopicNamespace.Should().Be("chat");
        change.ResourceKind.Should().Be("Channel");
        change.ResourceId.Should().Be(SpaceId);
        change.StreamKey.Should().Be($"chat:Channel:{SpaceId:N}");
        change.ChangeKind.Should().Be("chat.message.attachment.changed");
        change.PayloadContract.Should().Be("chat.attachment.v1");
        change.Payload.GetProperty("messageId").GetGuid().Should().Be(fact.MessageId);
        change.Payload.GetProperty("change").GetString().Should().Be("added");
        change.Payload.GetProperty("attachmentId").GetGuid().Should().Be(fact.AttachmentId);
        change.Payload.GetProperty("fileName").GetString().Should().Be("report.pdf");
        change.Payload.GetProperty("sizeBytes").GetInt64().Should().Be(1234);
        change.Payload.GetProperty("contentType").GetString().Should().Be("application/pdf");
        change.Payload.GetProperty("uploadedBy").GetGuid().Should().Be(fact.UploadedBy!.Value);
        change.Payload.GetProperty("downloadPath").GetString().Should()
            .Be($"/api/v1/workspaces/{fact.WorkspaceId}/chat/messages/{fact.MessageId}/attachments/{fact.AttachmentId}/content");
        change.Payload.GetProperty("detailsOmitted").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void A_removal_carries_only_the_ids_and_the_change()
    {
        var fact = Removed();

        var change = ChatRealtime.ForAttachmentChanged(fact, Now);

        change.Payload.GetProperty("change").GetString().Should().Be("removed");
        change.Payload.GetProperty("attachmentId").GetGuid().Should().Be(fact.AttachmentId);
        change.Payload.GetProperty("fileName").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        change.Payload.GetProperty("downloadPath").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public void The_payload_never_carries_bytes_and_stays_under_the_frame_budget()
    {
        var change = ChatRealtime.ForAttachmentChanged(Added(), Now);

        change.Payload.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            ["messageId", "change", "attachmentId", "fileName", "sizeBytes", "contentType", "uploadedBy", "createdAt", "downloadPath", "detailsOmitted"]);
        Encoding.UTF8.GetByteCount(change.Payload.GetRawText()).Should().BeLessThan(ChatRealtime.MaxPayloadBytes);
    }

    [Fact]
    public void A_file_name_that_does_not_fit_drops_the_details_and_the_reader_refetches()
    {
        var change = ChatRealtime.ForAttachmentChanged(Added(new string('é', 1024) + new string('€', 1024)), Now);

        Encoding.UTF8.GetByteCount(change.Payload.GetRawText()).Should().BeLessThan(4096);
        change.Payload.GetProperty("detailsOmitted").GetBoolean().Should().BeTrue();
        change.Payload.GetProperty("fileName").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        change.Payload.GetProperty("change").GetString().Should().Be("added");
    }
}
