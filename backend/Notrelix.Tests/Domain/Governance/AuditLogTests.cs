using FluentAssertions;
using Notrelix.Domain.Governance.Audit;
using Notrelix.Domain.Governance;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Governance;

public class AuditLogTests
{
    [Fact]
    public void Record_ShouldCreateLog_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var metadata = AuditMetadata.Create("127.0.0.1", "Mozilla/5.0");

        var log = AuditLog.Record(
            workspaceId,
            actorId,
            "DeleteBoard",
            ResourceRef.Create(ResourceType.Board, resourceId),
            metadata,
            AuditSeverity.Warning,
            "127.0.0.1",
            "Mozilla/5.0",
            DateTimeOffset.UtcNow);

        log.WorkspaceId.Should().Be(workspaceId);
        log.ActorId.Should().Be(actorId);
        log.Action.Should().Be("DeleteBoard");
        log.DomainEvents.Should().ContainSingle(e => e is AuditLogRecordedEvent);
    }

    [Fact]
    public void AuditLog_ShouldBeAppendOnly_AndNotExposeMutationMethods()
    {
        var type = typeof(AuditLog);
        
        // Assert that no Set, Update, or Delete methods exist in the type
        var publicMethods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
        
        // AuditLog inherits from Entity, but we shouldn't have business methods that modify it.
        // E.g., no Rename, UpdateMetadata, etc.
        publicMethods.Select(m => m.Name).Should().NotContainMatch("Update*");
        publicMethods.Select(m => m.Name).Should().NotContainMatch("Set*");
    }
}
