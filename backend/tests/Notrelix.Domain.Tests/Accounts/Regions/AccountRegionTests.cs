using FluentAssertions;
using Notrelix.Domain.Accounts.Regions;

namespace Notrelix.Domain.Tests.Accounts.Regions;

public class AccountRegionTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSetProperties()
    {
        var region = new AccountRegion(AccountId, "us-east-1");

        region.AccountId.Should().Be(AccountId);
        region.RegionCode.Should().Be("us-east-1");
        region.DataResidencyMode.Should().Be("Default");
        region.IsPrimary.Should().BeFalse();
        region.MigrationStatus.Should().BeNull();
    }

    [Fact]
    public void Create_WithIsPrimary_ShouldSetPrimary()
    {
        var region = new AccountRegion(AccountId, "eu-west-1", isPrimary: true);

        region.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => new AccountRegion(Guid.Empty, "us-east-1");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyRegionCode_ShouldThrow()
    {
        var act = () => new AccountRegion(AccountId, "");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void SetAsPrimary_ShouldSetIsPrimary()
    {
        var region = new AccountRegion(AccountId, "us-east-1");

        region.SetAsPrimary();

        region.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void UnsetPrimary_ShouldClearIsPrimary()
    {
        var region = new AccountRegion(AccountId, "us-east-1", isPrimary: true);

        region.UnsetPrimary();

        region.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void StartMigration_ShouldSetMigrationStatus()
    {
        var region = new AccountRegion(AccountId, "us-east-1");

        region.StartMigration();

        region.MigrationStatus.Should().Be("InProgress");
    }

    [Fact]
    public void CompleteMigration_ShouldSetMigrationStatus()
    {
        var region = new AccountRegion(AccountId, "us-east-1");
        region.StartMigration();

        region.CompleteMigration();

        region.MigrationStatus.Should().Be("Completed");
    }

    [Fact]
    public void Create_ShouldTrimRegionCode()
    {
        var region = new AccountRegion(AccountId, "  us-east-1  ");

        region.RegionCode.Should().Be("us-east-1");
    }
}
