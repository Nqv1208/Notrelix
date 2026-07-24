using FluentAssertions;

namespace Notrelix.Domain.Tests.SharedKernel;

public class ColorTests
{
    [Fact]
    public void Create_ValidHex6_ShouldSucceed()
    {
        var color = Color.Create("#FF0000");
        color.Value.Should().Be("#FF0000");
    }

    [Fact]
    public void Create_ValidHex3_ShouldExpandToHex6()
    {
        var color = Color.Create("#F00");
        color.Value.Should().Be("#FF0000");
    }

    [Fact]
    public void Create_Lowercase_ShouldUppercase()
    {
        var color = Color.Create("#ff0000");
        color.Value.Should().Be("#FF0000");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrim()
    {
        var color = Color.Create("  #FF0000  ");
        color.Value.Should().Be("#FF0000");
    }

    [Fact]
    public void Create_InvalidFormat_ShouldThrow()
    {
        var act = () => Color.Create("red");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_Empty_ShouldThrow()
    {
        var act = () => Color.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameColor_ShouldBeEqual()
    {
        Color.Create("#FF0000").Should().Be(Color.Create("#ff0000"));
    }

    [Fact]
    public void Equality_Hex3AndHex6_ShouldBeEqual()
    {
        Color.Create("#F00").Should().Be(Color.Create("#FF0000"));
    }

    [Fact]
    public void Equality_DifferentColors_ShouldNotBeEqual()
    {
        Color.Create("#FF0000").Should().NotBe(Color.Create("#00FF00"));
    }
}

public class IconTests
{
    [Fact]
    public void FromEmoji_ShouldCreate()
    {
        var icon = Icon.FromEmoji("🚀");
        icon.Value.Should().Be("🚀");
        icon.Type.Should().Be(IconType.Emoji);
    }

    [Fact]
    public void FromEmoji_ShouldTrim()
    {
        var icon = Icon.FromEmoji("  🚀  ");
        icon.Value.Should().Be("🚀");
    }

    [Fact]
    public void FromEmoji_Empty_ShouldThrow()
    {
        var act = () => Icon.FromEmoji("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromName_ShouldLowercase()
    {
        var icon = Icon.FromName("Rocket");
        icon.Value.Should().Be("rocket");
        icon.Type.Should().Be(IconType.IconName);
    }

    [Fact]
    public void FromName_Empty_ShouldThrow()
    {
        var act = () => Icon.FromName("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SameValueAndType_ShouldBeEqual()
    {
        Icon.FromEmoji("🚀").Should().Be(Icon.FromEmoji("🚀"));
    }

    [Fact]
    public void Equality_DifferentType_ShouldNotBeEqual()
    {
        Icon.FromEmoji("rocket").Should().NotBe(Icon.FromName("rocket"));
    }
}

public class JsonValueTests
{
    [Fact]
    public void Create_ValidJson_ShouldSucceed()
    {
        var json = JsonValue.Create("{\"key\":\"value\"}");
        json.Value.Should().Be("{\"key\":\"value\"}");
    }

    [Fact]
    public void Create_ShouldCompact()
    {
        var json = JsonValue.Create("{ \"key\" : \"value\" }");
        json.Value.Should().Be("{\"key\":\"value\"}");
    }

    [Fact]
    public void Create_CompactEquality()
    {
        JsonValue.Create("{ \"a\": 1 }").Should().Be(JsonValue.Create("{\"a\":1}"));
    }

    [Fact]
    public void Create_InvalidJson_ShouldThrow()
    {
        var act = () => JsonValue.Create("{invalid}");
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Invalid JSON*");
    }

    [Fact]
    public void Create_InvalidJson_ShouldNotExposeParserMessage()
    {
        var act = () => JsonValue.Create("{invalid}");
        act.Should().Throw<BusinessRuleException>()
            .Where(e => !e.Message.Contains("'{' is invalid"));
    }

    [Fact]
    public void EmptyObject_ShouldReturnEmptyBraces()
    {
        JsonValue.EmptyObject().Value.Should().Be("{}");
    }

    [Fact]
    public void EmptyArray_ShouldReturnEmptyBrackets()
    {
        JsonValue.EmptyArray().Value.Should().Be("[]");
    }

    [Fact]
    public void Null_ShouldReturnNullLiteral()
    {
        JsonValue.Null().Value.Should().Be("null");
    }

    [Fact]
    public void Create_Array_ShouldSucceed()
    {
        var json = JsonValue.Create("[1,2,3]");
        json.Value.Should().Be("[1,2,3]");
    }
}

public class SecretRefTests
{
    [Fact]
    public void Create_ShouldTrim()
    {
        var secret = SecretRef.Create("  my-secret  ");
        secret.Value.Should().Be("my-secret");
    }

    [Fact]
    public void Create_Empty_ShouldThrow()
    {
        var act = () => SecretRef.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ToString_ShouldMask()
    {
        var secret = SecretRef.Create("super-secret");
        secret.ToString().Should().Be("[secret-ref]");
        secret.ToString().Should().NotContain("super-secret");
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        SecretRef.Create("abc").Should().Be(SecretRef.Create("abc"));
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        SecretRef.Create("abc").Should().NotBe(SecretRef.Create("def"));
    }
}

public class UrlTests
{
    [Fact]
    public void Create_ValidHttp_ShouldSucceed()
    {
        var url = Url.Create("http://example.com");
        url.Value.Should().Contain("example.com");
    }

    [Fact]
    public void Create_ValidHttps_ShouldSucceed()
    {
        var url = Url.Create("https://example.com/path?q=1");
        url.Value.Should().Contain("example.com");
    }

    [Fact]
    public void Create_ShouldNormalizeSchemeToLowercase()
    {
        var url = Url.Create("HTTP://EXAMPLE.COM");
        url.Value.Should().StartWith("http://");
    }

    [Fact]
    public void Create_ShouldNormalizeHostToLowercase()
    {
        var url = Url.Create("https://EXAMPLE.COM/Path");
        url.Value.Should().Contain("example.com");
    }

    [Fact]
    public void Create_FtpScheme_ShouldThrow()
    {
        var act = () => Url.Create("ftp://example.com");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_NotAbsolute_ShouldThrow()
    {
        var act = () => Url.Create("/relative/path");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_Empty_ShouldThrow()
    {
        var act = () => Url.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameUrl_ShouldBeEqual()
    {
        Url.Create("https://example.com").Should().Be(Url.Create("https://example.com"));
    }

    [Fact]
    public void Equality_CaseNormalized_ShouldBeEqual()
    {
        Url.Create("HTTPS://EXAMPLE.COM").Should().Be(Url.Create("https://example.com"));
    }
}

public class DateRangeTests
{
    [Fact]
    public void Create_ValidRange_ShouldSucceed()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
        var range = DateRange.Create(start, end);
        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void Create_OpenEnded_ShouldSucceed()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var range = DateRange.Create(start);
        range.End.Should().BeNull();
    }

    [Fact]
    public void Create_StartAfterEnd_ShouldThrow()
    {
        var start = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var act = () => DateRange.Create(start, end);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_DefaultStart_ShouldThrow()
    {
        var act = () => DateRange.Create(default);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_MinValueStart_ShouldThrow()
    {
        var act = () => DateRange.Create(DateTimeOffset.MinValue);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameRange_ShouldBeEqual()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateRange.Create(start).Should().Be(DateRange.Create(start));
    }
}

public class ResourceTypeContractTests
{
    [Fact]
    public void ResourceType_ShouldHaveExplicitValues()
    {
        var values = Enum.GetValues<ResourceType>();
        var numericValues = values.Select(v => (int)v).ToList();

        // All values should be unique
        numericValues.Distinct().Should().HaveCount(numericValues.Count);
    }

    [Theory]
    [InlineData(ResourceType.Account, 0)]
    [InlineData(ResourceType.Workspace, 1)]
    [InlineData(ResourceType.Board, 10)]
    [InlineData(ResourceType.BoardItem, 13)]
    [InlineData(ResourceType.Page, 40)]
    [InlineData(ResourceType.Block, 41)]
    [InlineData(ResourceType.Comment, 60)]
    [InlineData(ResourceType.AutomationRule, 70)]
    [InlineData(ResourceType.Subscription, 110)]
    [InlineData(ResourceType.User, 120)]
    [InlineData(ResourceType.External, 200)]
    public void ResourceType_SnapshotValues(ResourceType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}
