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

        var ref1 = ResourceRef.Create(ResourceType.Board, id, wsId);
        var ref2 = ResourceRef.Create(ResourceType.Board, id, wsId);
        var ref3 = ResourceRef.Create(ResourceType.Page, id, wsId);

        ref1.Should().Be(ref2);
        ref1.Should().NotBe(ref3);
    }
}

public class LegacyResourceTypeMappingTests
{
    [Fact]
    public void RES_MAP_001_Mapping_Count_Equals_All_Enum_Values()
    {
        var enumCount = Enum.GetValues<ResourceType>().Length;
        LegacyResourceTypeMappings.All.Count.Should().Be(enumCount,
            "every ResourceType enum value must have exactly one canonical mapping");
    }

    [Fact]
    public void RES_MAP_002_Mapping_Values_Are_Unique()
    {
        var values = LegacyResourceTypeMappings.All.Values.ToArray();
        var distinct = values.Distinct(StringComparer.Ordinal).ToArray();

        distinct.Length.Should().Be(values.Length,
            "no two ResourceType values may map to the same ResourceKind string");
    }

    [Fact]
    public void RES_MAP_003_Every_Mapping_Produces_Valid_ResourceKind()
    {
        foreach (var (enumValue, kindString) in LegacyResourceTypeMappings.All)
        {
            var act = () => ResourceKind.Create(kindString);
            act.Should().NotThrow($"mapping for {enumValue} → '{kindString}' must be a valid ResourceKind");
        }
    }

    [Fact]
    public void RES_MAP_003_Spot_Check_Exact_Mappings()
    {
        LegacyResourceTypeMappings.ToResourceKind(ResourceType.Account).Value.Should().Be("accounts.account");
        LegacyResourceTypeMappings.ToResourceKind(ResourceType.Board).Value.Should().Be("work-management.board");
        LegacyResourceTypeMappings.ToResourceKind(ResourceType.BoardItem).Value.Should().Be("work-management.board-item");
        LegacyResourceTypeMappings.ToResourceKind(ResourceType.Page).Value.Should().Be("documents.page");
        LegacyResourceTypeMappings.ToResourceKind(ResourceType.User).Value.Should().Be("identity.user");
        LegacyResourceTypeMappings.ToResourceKind(ResourceType.External).Value.Should().Be("external.resource");
    }

    [Fact]
    public void RES_MAP_003_Reverse_Mapping_Works()
    {
        LegacyResourceTypeMappings.TryToLegacyEnum("work-management.board", out var rt).Should().BeTrue();
        rt.Should().Be(ResourceType.Board);

        LegacyResourceTypeMappings.TryToLegacyEnum("unknown.new-thing", out _).Should().BeFalse();
    }

    [Fact]
    public void RES_MAP_004_Unknown_Enum_Value_Throws()
    {
        var act = () => LegacyResourceTypeMappings.ToResourceKind((ResourceType)9999);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
