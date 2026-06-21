using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Governance.Security;
using Notrelix.Domain.Governance.Security.Events;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Governance;

public class SecurityEventTests
{
    [Fact]
    public void Record_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var metadata = SecurityEventMetadata.Create(JsonValue.EmptyObject());
        var now = DateTimeOffset.UtcNow;

        var evt = SecurityEvent.Record(workspaceId, SecurityEventType.FailedLogin, SecuritySeverity.Medium, "Failed login", metadata, now);

        evt.WorkspaceId.Should().Be(workspaceId);
        evt.Type.Should().Be(SecurityEventType.FailedLogin);
        evt.Severity.Should().Be(SecuritySeverity.Medium);
        evt.Title.Should().Be("Failed login");
        evt.Metadata.Should().Be(metadata);
        evt.OccurredAt.Should().Be(now);
        evt.DomainEvents.Should().ContainSingle(e => e is SecurityEventRecordedEvent);
    }

    [Fact]
    public void Record_WithEmptyWorkspaceId_ShouldThrow()
    {
        var metadata = SecurityEventMetadata.Create(JsonValue.EmptyObject());
        var act = () => SecurityEvent.Record(Guid.Empty, SecurityEventType.FailedLogin, SecuritySeverity.Low, "Test", metadata, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Record_WithNullTitle_ShouldThrow()
    {
        var metadata = SecurityEventMetadata.Create(JsonValue.EmptyObject());
        var act = () => SecurityEvent.Record(Guid.NewGuid(), SecurityEventType.FailedLogin, SecuritySeverity.Low, null!, metadata, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Record_WithDifferentTypes_ShouldSetType()
    {
        var metadata = SecurityEventMetadata.Create(JsonValue.EmptyObject());
        var evt = SecurityEvent.Record(Guid.NewGuid(), SecurityEventType.DataExport, SecuritySeverity.Critical, "Data export", metadata, DateTimeOffset.UtcNow);
        evt.Type.Should().Be(SecurityEventType.DataExport);
    }

    [Fact]
    public void Record_WithDifferentSeverities_ShouldSetSeverity()
    {
        var metadata = SecurityEventMetadata.Create(JsonValue.EmptyObject());
        var evt = SecurityEvent.Record(Guid.NewGuid(), SecurityEventType.PermissionDenied, SecuritySeverity.High, "Alert", metadata, DateTimeOffset.UtcNow);
        evt.Severity.Should().Be(SecuritySeverity.High);
    }
}
