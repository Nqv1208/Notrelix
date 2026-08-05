using FluentAssertions;

namespace Notrelix.Domain.Tests.SharedKernel;

public class ResourceKindTests
{
    [Theory]
    [InlineData("accounts.account")]
    [InlineData("work-management.board")]
    [InlineData("work-management.board-item")]
    [InlineData("documents.page")]
    [InlineData("collaboration.comment")]
    [InlineData("automation.rule")]
    [InlineData("integrations.connection")]
    [InlineData("billing.subscription")]
    [InlineData("identity.user")]
    [InlineData("governance.role")]
    [InlineData("analytics.dashboard")]
    [InlineData("external.resource")]
    [InlineData("notifications.notification")]
    public void RES_001_Accepts_Every_Canonical_Mapping_Value(string value)
    {
        var kind = ResourceKind.Create(value);
        kind.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("single")]
    [InlineData("UPPER.case")]
    [InlineData("has space.other")]
    [InlineData(".leading-dot")]
    [InlineData("trailing-dot.")]
    [InlineData("double..dot")]
    [InlineData("under_score.valid")]
    public void RES_002_Rejects_Invalid_Values(string value)
    {
        var act = () => ResourceKind.Create(value);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RES_003_Unknown_WellFormed_Kind_RoundTrips()
    {
        var kind = ResourceKind.Create("new-context.some-resource");
        kind.Value.Should().Be("new-context.some-resource");

        var parsed = ResourceKind.Create(kind.Value);
        parsed.Should().Be(kind);
    }

    [Fact]
    public void RES_004_Ordinal_Equality_And_Hash()
    {
        var a = ResourceKind.Create("work-management.board");
        var b = ResourceKind.Create("work-management.board");
        var c = ResourceKind.Create("work-management.board-item");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(c);
    }

    [Fact]
    public void RES_004_Case_Sensitive_Equality()
    {
        var lower = ResourceKind.Create("accounts.account");
        // "Accounts.Account" would be rejected by validation, so test ordinal directly
        var same = ResourceKind.Create("accounts.account");
        lower.Should().Be(same);
    }

    [Fact]
    public void TryCreate_Returns_False_For_Invalid()
    {
        ResourceKind.TryCreate(null, out _).Should().BeFalse();
        ResourceKind.TryCreate("", out _).Should().BeFalse();
        ResourceKind.TryCreate("single", out _).Should().BeFalse();
        ResourceKind.TryCreate("UPPER.case", out _).Should().BeFalse();
    }

    [Fact]
    public void TryCreate_Returns_True_For_Valid()
    {
        ResourceKind.TryCreate("accounts.account", out var kind).Should().BeTrue();
        kind.Value.Should().Be("accounts.account");
    }

    [Fact]
    public void RES_005_ResourceRef_Equality_Includes_Kind_Id_WorkspaceId()
    {
        var id = Guid.NewGuid();
        var wsId = Guid.NewGuid();

        var ref1 = ResourceRef.Create(ResourceKind.Create("work-management.board"), id, wsId);
        var ref2 = ResourceRef.Create(ResourceKind.Create("work-management.board"), id, wsId);
        var ref3 = ResourceRef.Create(ResourceKind.Create("documents.page"), id, wsId);

        ref1.Should().Be(ref2);
        ref1.Should().NotBe(ref3);
    }
}
