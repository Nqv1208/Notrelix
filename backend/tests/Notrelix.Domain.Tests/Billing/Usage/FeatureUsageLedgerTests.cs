using FluentAssertions;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Domain.Tests.Billing;

public class FeatureUsageLedgerTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var now = DateTimeOffset.UtcNow;
        var ledger = FeatureUsageLedger.Create(
            Guid.NewGuid(),
            "BOARD_COUNT",
            5,
            Guid.NewGuid(),
            "board-123",
            "Created new board",
            now);

        ledger.FeatureCode.Should().Be("BOARD_COUNT");
        ledger.Delta.Should().Be(5);
        ledger.Note.Should().Be("Created new board");
        ledger.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void Create_ShouldNormalizeFeatureCode()
    {
        var ledger = FeatureUsageLedger.Create(
            Guid.NewGuid(),
            " Board_Limit ",
            1,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);

        ledger.FeatureCode.Should().Be("BOARD_LIMIT");
    }

    [Fact]
    public void Create_WithNegativeDelta_ShouldSucceed()
    {
        var ledger = FeatureUsageLedger.Create(
            Guid.NewGuid(),
            "API_CALLS",
            -3,
            Guid.NewGuid(),
            null,
            "Released",
            DateTimeOffset.UtcNow);

        ledger.Delta.Should().Be(-3);
    }

    [Fact]
    public void Create_WithEmptyFeatureCode_ShouldThrow()
    {
        var act = () => FeatureUsageLedger.Create(
            Guid.NewGuid(),
            "",
            1,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullFeatureCode_ShouldThrow()
    {
        var act = () => FeatureUsageLedger.Create(
            Guid.NewGuid(),
            null!,
            1,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }
}
