using Notrelix.Infrastructure.Identity.Mfa;

namespace Notrelix.Infrastructure.Tests.Identity.Mfa;

public class MfaRecoveryCodeGeneratorTests
{
    private readonly MfaRecoveryCodeGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsRequestedCountInExpectedDisplayFormat()
    {
        var codes = _generator.Generate(8);

        codes.Should().HaveCount(8);
        codes.Should().OnlyContain(c => c != null && c.Length == 24);
        codes.Should().OnlyContain(c => System.Text.RegularExpressions.Regex.IsMatch(
            c, "^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$"));
    }

    [Fact]
    public void Generate_CodesAreUnique()
    {
        var codes = _generator.Generate(8);

        codes.Distinct().Should().HaveCount(8);
    }

    [Fact]
    public void Generate_WithNonPositiveCount_Throws()
    {
        var act = () => _generator.Generate(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Hash_IsDeterministicLowercaseHex()
    {
        var code = "ABCD-1234-EFGH-5678-IJKL";

        var hash1 = _generator.Hash(code);
        var hash2 = _generator.Hash(code);

        hash1.Should().Be(hash2);
        hash1.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Hash_CanonicalizesDashesAndCaseBeforeHashing()
    {
        var a = _generator.Hash("abcd-1234-efgh-5678-ijkl");
        var b = _generator.Hash("ABCD1234EFGH5678IJKL");

        a.Should().Be(b);
    }

    [Fact]
    public void Hash_NeverEqualsPlaintextCode()
    {
        var code = "ABCD-1234-EFGH-5678-IJKL";

        _generator.Hash(code).Should().NotBe(code);
    }
}