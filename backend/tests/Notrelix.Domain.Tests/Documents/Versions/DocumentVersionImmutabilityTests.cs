using System.Linq;
using FluentAssertions;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.Tests.Freeze;
using Xunit;

namespace Notrelix.Domain.Tests.Documents.Versions;

public class DocumentVersionImmutabilityTests
{
    [Fact]
    public void Create_Properties_ShouldMatchInput()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        var snapshot = DocumentSnapshot.Create(JsonValue.EmptyObject());
        var createdBy = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

        var version = DocumentVersion.Create(accountId, workspaceId, pageId, 1, snapshot, createdBy, createdAt);

        version.AccountId.Should().Be(accountId);
        version.WorkspaceId.Should().Be(workspaceId);
        version.PageId.Should().Be(pageId);
        version.VersionNumber.Should().Be(1);
        version.Snapshot.Should().Be(snapshot);
    }

    [Fact]
    public void Create_Snapshot_ShouldBeSameReference()
    {
        var snapshot = DocumentSnapshot.Create(JsonValue.EmptyObject());
        var version = DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, snapshot, Guid.NewGuid(), DateTimeOffset.UtcNow);
        version.Snapshot.Should().BeSameAs(snapshot);
    }

    [Fact]
    public void DomainEvent_ShouldContainAccountId()
    {
        var accountId = Guid.NewGuid();
        var version = DocumentVersion.Create(accountId, Guid.NewGuid(), Guid.NewGuid(), 1,
            DocumentSnapshot.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        version.DomainEvents.OfType<DocumentVersionCreatedDomainEvent>()
            .Should().ContainSingle().Which.AccountId.Should().Be(accountId);
    }

    [Fact]
    public void DomainEvent_ShouldContainVersionNumber()
    {
        const int versionNumber = 5;
        var version = DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), versionNumber,
            DocumentSnapshot.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        version.DomainEvents.OfType<DocumentVersionCreatedDomainEvent>()
            .Should().ContainSingle().Which.VersionNumber.Should().Be(versionNumber);
    }

    [CoversMutation(typeof(DocumentVersion), "ApplyRestore(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void ApplyRestore_ShouldIncrementVersion()
    {
        var version = DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            DocumentSnapshot.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = version.Version;
        version.ApplyRestore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        version.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(DocumentVersion), "ApplyRestore(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void ApplyRestore_ShouldRaiseEvent()
    {
        var version = DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            DocumentSnapshot.Create(JsonValue.EmptyObject()), Guid.NewGuid(), DateTimeOffset.UtcNow);
        version.ApplyRestore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        version.DomainEvents.OfType<DocumentVersionRestoredDomainEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Create_DeterministicTimestamps()
    {
        var ts = new DateTimeOffset(2026, 6, 15, 14, 30, 0, TimeSpan.Zero);
        var version = DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            DocumentSnapshot.Create(JsonValue.EmptyObject()), Guid.NewGuid(), ts);
        version.CreatedAt.Should().Be(ts);
    }
}
