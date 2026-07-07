using Notrelix.Application.Common.Caching;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure.Tests.Options;

public class CacheKeyOptionsValidatorTests
{
    private static CacheKeyOptionsValidator CreateValidator() => new();

    private static CacheKeyOptions ValidOptions() => new()
    {
        Environment = "production",
        Prefix = "notrelix",
        SchemaVersion = 1
    };

    [Fact]
    public void ValidOptions_ReturnsSuccess()
    {
        var result = CreateValidator().Validate(null, ValidOptions());
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void EmptyEnvironment_ReturnsFail()
    {
        var options = ValidOptions();
        options.Environment = "";

        var result = CreateValidator().Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Environment"));
    }

    [Fact]
    public void EnvironmentWithColon_ReturnsFail()
    {
        var options = ValidOptions();
        options.Environment = "dev:1";

        var result = CreateValidator().Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Environment") && f.Contains(":"));
    }

    [Fact]
    public void WhitespaceEnvironment_ReturnsFail()
    {
        var options = ValidOptions();
        options.Environment = "   ";

        var result = CreateValidator().Validate(null, options);
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void EmptyPrefix_ReturnsFail()
    {
        var options = ValidOptions();
        options.Prefix = "";

        var result = CreateValidator().Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Prefix"));
    }

    [Fact]
    public void PrefixWithColon_ReturnsFail()
    {
        var options = ValidOptions();
        options.Prefix = "my:app";

        var result = CreateValidator().Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Prefix") && f.Contains(":"));
    }

    [Fact]
    public void SchemaVersionZero_ReturnsFail()
    {
        var options = ValidOptions();
        options.SchemaVersion = 0;

        var result = CreateValidator().Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("SchemaVersion"));
    }

    [Fact]
    public void SchemaVersionNegative_ReturnsFail()
    {
        var options = ValidOptions();
        options.SchemaVersion = -1;

        var result = CreateValidator().Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("SchemaVersion"));
    }
}
