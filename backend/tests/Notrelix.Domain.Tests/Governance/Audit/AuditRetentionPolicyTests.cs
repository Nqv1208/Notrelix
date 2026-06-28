using FluentAssertions;
using Notrelix.Domain.Governance.Audit;

namespace Notrelix.Domain.Tests.Governance;

public class AuditRetentionPolicyTests
{
    [Fact]
    public void Create_WithDefaults_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();

        var policy = AuditRetentionPolicy.Create(workspaceId);

        policy.WorkspaceId.Should().Be(workspaceId);
        policy.RetentionDays.Should().Be(365);
        policy.ExportBeforeDelete.Should().BeFalse();
    }

    [Fact]
    public void Create_WithCustomValues_ShouldSetProperties()
    {
        var workspaceId = Guid.NewGuid();

        var policy = AuditRetentionPolicy.Create(workspaceId, retentionDays: 90, exportBeforeDelete: true);

        policy.RetentionDays.Should().Be(90);
        policy.ExportBeforeDelete.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => AuditRetentionPolicy.Create(Guid.Empty);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNegativeRetentionDays_ShouldThrow()
    {
        var act = () => AuditRetentionPolicy.Create(Guid.NewGuid(), retentionDays: -1);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithZeroRetentionDays_ShouldThrow()
    {
        var act = () => AuditRetentionPolicy.Create(Guid.NewGuid(), retentionDays: 0);
        act.Should().Throw<BusinessRuleException>();
    }
}
