using Notrelix.Application.Features.Identity.Auth.Queries.IdentifySignIn;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Queries;

public class IdentifySignInTests
{
    private readonly IdentifySignInQueryHandler _handler = new();

    [Theory]
    [InlineData("known@example.com")]
    [InlineData("nobody-has-this@example.org")]
    [InlineData("someone@minhan.vn")]
    public async Task Answers_Password_ForAnyEmail_SoNoAccountCanBeProbed(string email)
    {
        var result = await _handler.Handle(new IdentifySignInQuery(email), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().Be(new SignInMethodDto(SignInMethods.Password, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validator_RejectsAnInvalidEmail(string email) =>
        new IdentifySignInQueryValidator().Validate(new IdentifySignInQuery(email)).IsValid.Should().BeFalse();

    [Fact]
    public void Validator_AcceptsAnEmail() =>
        new IdentifySignInQueryValidator().Validate(new IdentifySignInQuery("a@b.co")).IsValid.Should().BeTrue();
}
