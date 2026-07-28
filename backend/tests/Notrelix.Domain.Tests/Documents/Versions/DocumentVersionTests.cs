using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Domain.Tests.Documents;

[CoversAggregate(typeof(DocumentVersion))]
public class DocumentVersionTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        var snapshot = DocumentSnapshot.Create(JsonValue.EmptyObject());
        var now = DateTimeOffset.UtcNow;

        var version = DocumentVersion.Create(Guid.NewGuid(), workspaceId, pageId, 1, snapshot, Guid.NewGuid(), now);

        version.WorkspaceId.Should().Be(workspaceId);
        version.PageId.Should().Be(pageId);
        version.VersionNumber.Should().Be(1);
        version.Snapshot.Should().Be(snapshot);
        version.DomainEvents.Should().ContainSingle(e => e is DocumentVersionCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithChangeSummary_ShouldTrim()
    {
        var snapshot = DocumentSnapshot.Create(JsonValue.EmptyObject());
        var version = DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, snapshot, Guid.NewGuid(), DateTimeOffset.UtcNow, "  summary  ");
        version.ChangeSummary.Should().Be("summary");
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var snapshot = DocumentSnapshot.Create(JsonValue.EmptyObject());
        var act = () => DocumentVersion.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 1, snapshot, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyPageId_ShouldThrow()
    {
        var snapshot = DocumentSnapshot.Create(JsonValue.EmptyObject());
        var act = () => DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 1, snapshot, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithZeroVersionNumber_ShouldThrow()
    {
        var snapshot = DocumentSnapshot.Create(JsonValue.EmptyObject());
        var act = () => DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, snapshot, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullSnapshot_ShouldThrow()
    {
        var act = () => DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, null!, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ApplyRestore_ShouldRaiseEvent()
    {
        var version = CreateVersion();
        var now = DateTimeOffset.UtcNow;

        version.ApplyRestore(Guid.NewGuid(), now);

        version.DomainEvents.Should().ContainSingle(e => e is DocumentVersionRestoredDomainEvent);
    }

    private static DocumentVersion CreateVersion()
    {
        return DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DocumentSnapshot.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
