using FluentAssertions;
using Notrelix.Domain.Collaboration.Attachments;

namespace Notrelix.Domain.Tests.Collaboration;

/// <summary>
/// A declared two-step upload: it owns its key and facts, it expires at its window, and a commit can be recorded once.
/// </summary>
public class PendingAttachmentUploadTests
{
    private static readonly DateTimeOffset Created = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Message = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();

    private static PendingAttachmentUpload Pending(long size = 1024, long ceiling = 4096) => PendingAttachmentUpload.Create(
        Account,
        Workspace,
        ResourceRef.Create(ResourceKind.Create("chat.message"), Message, Workspace),
        $"{Account:N}/{Workspace:N}/attachments/{Guid.NewGuid():N}",
        "photo.png",
        size,
        "image/png",
        ceiling,
        voice: false,
        durationHintMs: null,
        waveform: null,
        Actor,
        Created,
        Created.AddMinutes(15));

    [Fact]
    public void It_is_workspace_scoped_and_records_its_declared_facts()
    {
        var pending = Pending();

        pending.WorkspaceId.Should().Be(Workspace);
        pending.AccountId.Should().Be(Account);
        pending.DeclaredSize.Should().Be(1024);
        pending.CeilingBytes.Should().Be(4096);
        pending.IsCommitted.Should().BeFalse();
        pending.Should().BeAssignableTo<IWorkspaceScoped>();
    }

    [Fact]
    public void It_is_expired_from_its_window_end_on()
    {
        var pending = Pending();

        pending.IsExpiredAt(Created.AddMinutes(14)).Should().BeFalse();
        pending.IsExpiredAt(Created.AddMinutes(15)).Should().BeTrue();
    }

    [Fact]
    public void A_declared_size_over_the_ceiling_is_refused()
    {
        var act = () => Pending(size: 5000, ceiling: 4096);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void A_commit_is_recorded_once_and_a_second_one_is_refused()
    {
        var pending = Pending();
        var attachmentId = Guid.NewGuid();

        pending.MarkCommitted(attachmentId, Created.AddMinutes(1));

        pending.IsCommitted.Should().BeTrue();
        pending.AttachmentId.Should().Be(attachmentId);
        var again = () => pending.MarkCommitted(Guid.NewGuid(), Created.AddMinutes(2));
        again.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void A_window_must_end_after_creation()
    {
        var act = () => PendingAttachmentUpload.Create(
            Account, Workspace, ResourceRef.Create(ResourceKind.Create("chat.message"), Message, Workspace),
            $"{Account:N}/{Workspace:N}/attachments/{Guid.NewGuid():N}", "photo.png", 10, "image/png", 4096,
            voice: false, durationHintMs: null, waveform: null, Actor, Created, Created);

        act.Should().Throw<ArgumentException>();
    }
}
