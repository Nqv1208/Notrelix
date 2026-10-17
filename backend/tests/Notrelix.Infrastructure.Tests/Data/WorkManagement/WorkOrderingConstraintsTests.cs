using Microsoft.EntityFrameworkCore;
using Npgsql;
using Notrelix.Infrastructure.Data.WorkManagement;

namespace Notrelix.Infrastructure.Tests.Data.WorkManagement;

/// <summary>
/// ORDER-CONFLICT-001 (WM-V3-ORDER-003). Only a unique violation of a named P3
/// ordering constraint is classified as an ordering conflict; other unique
/// violations and other database errors keep their existing mapping.
/// </summary>
public sealed class WorkOrderingConstraintsTests
{
    [Fact]
    public void NamedOrderingConstraintViolation_IsAnOrderingConflict()
    {
        WorkOrderingConstraints.IsOrderingViolation(Violation(PostgresErrorCodes.UniqueViolation, "ux_work_board_groups_board_position_active"))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_workspace_members_user")]
    [InlineData(PostgresErrorCodes.UniqueViolation, null)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "ux_work_board_groups_board_position_active")]
    public void OtherViolations_AreNotOrderingConflicts(string sqlState, string? constraint)
    {
        WorkOrderingConstraints.IsOrderingViolation(Violation(sqlState, constraint)).Should().BeFalse();
    }

    [Fact]
    public void TheSixP3ScopesHaveNamedConstraints()
    {
        WorkOrderingConstraints.Names.Should().HaveCount(6);
    }

    private static DbUpdateException Violation(string sqlState, string? constraintName) =>
        new("save failed", new PostgresException("violation", "ERROR", "ERROR", sqlState, constraintName: constraintName));
}
