using FluentAssertions;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Governance.AccessChanges;

namespace Notrelix.Domain.Tests.Governance;

public class ResourceAccessChangeTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 23, 8, 0, 0, TimeSpan.Zero);
    private static readonly ResourceKind Board = ResourceKind.Create("work-management.board");

    private static ResourceAccessChange Record(ResourceAccessChangeKind kind, string? subjectType, Guid? subjectId) =>
        ResourceAccessChange.Record(Guid.NewGuid(), Guid.NewGuid(), Board, Guid.NewGuid(), kind, Guid.NewGuid(), subjectType, subjectId, null, "editor", At);

    [Theory]
    [InlineData(ResourceAccessChangeKind.Added)]
    [InlineData(ResourceAccessChangeKind.Changed)]
    [InlineData(ResourceAccessChangeKind.Removed)]
    public void ASubjectChange_KeepsItsSubject_AndExpiresAfterNinetyDays(ResourceAccessChangeKind kind)
    {
        var subject = Guid.NewGuid();

        var change = Record(kind, "Team", subject);

        change.SubjectType.Should().Be("Team");
        change.SubjectId.Should().Be(subject);
        change.RetentionUntil.Should().Be(At.AddDays(90));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Guest", "00000000-0000-0000-0000-000000000001")]
    [InlineData("User", null)]
    public void ASubjectChange_WithoutAValidSubject_IsRefused(string? type, string? id)
    {
        var act = () => Record(ResourceAccessChangeKind.Added, type, id is null ? null : Guid.Parse(id));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(GovernanceRuleCodes.Governance_AccessChange_SubjectRequired);
    }

    [Theory]
    [InlineData(ResourceAccessChangeKind.MadePrivate)]
    [InlineData(ResourceAccessChangeKind.VisibilityChanged)]
    public void AVisibilityChange_DropsAnySubject(ResourceAccessChangeKind kind)
    {
        var change = Record(kind, "User", Guid.NewGuid());

        change.SubjectType.Should().BeNull();
        change.SubjectId.Should().BeNull();
    }
}
