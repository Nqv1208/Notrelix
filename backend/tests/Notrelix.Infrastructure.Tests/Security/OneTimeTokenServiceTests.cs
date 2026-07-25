using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Infrastructure.Security.Tokens;

namespace Notrelix.Infrastructure.Tests.Security;

public sealed class OneTimeTokenServiceTests
{
    private readonly OneTimeTokenService _service = new();

    [Fact]
    public void Generate_AndParse_ShouldRoundTripVersionWithoutDatabaseLookup()
    {
        var issued = _service.Generate(TokenPurpose.EmailVerification);

        issued.RawToken.Should().StartWith("v1.");
        issued.HashVersion.Should().Be(1);

        var parsed = _service.ParseAndHash(
            issued.RawToken,
            TokenPurpose.EmailVerification);

        parsed.TokenHash.Should().Be(issued.TokenHash);
        parsed.HashVersion.Should().Be(issued.HashVersion);
    }

    [Fact]
    public void Parse_WithUnsupportedVersion_ShouldFailGenerically()
    {
        var action = () => _service.ParseAndHash(
            "v2.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            TokenPurpose.EmailVerification);

        action.Should().Throw<InvalidOneTimeTokenException>()
            .WithMessage("The token is invalid or expired.");
    }

    [Fact]
    public void Parse_ShouldBindHashToPurpose()
    {
        var issued = _service.Generate(TokenPurpose.EmailVerification);

        var parsed = _service.ParseAndHash(
            issued.RawToken,
            TokenPurpose.WorkspaceInvitation);

        parsed.TokenHash.Should().NotBe(issued.TokenHash);
    }

    [Fact]
    public void Parse_TooLongToken_ShouldFail()
    {
        var action = () => _service.ParseAndHash(
            "v1." + new string('a', 254),
            TokenPurpose.EmailVerification);

        action.Should().Throw<InvalidOneTimeTokenException>();
    }
}
