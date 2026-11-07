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

    [Fact]
    public void Validator_TakesAnEmailOfExactly256Characters_AndRejectsOneMore()
    {
        var domain = "@example.com";
        var atLimit = new string('a', 256 - domain.Length) + domain;
        var validator = new IdentifySignInQueryValidator();

        atLimit.Length.Should().Be(256);
        validator.Validate(new IdentifySignInQuery(atLimit)).IsValid.Should().BeTrue();
        validator.Validate(new IdentifySignInQuery("a" + atLimit)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Retrying_AnswersTheSameAgain_AndTreatsCaseAndPaddingAlike()
    {
        var first = await _handler.Handle(new IdentifySignInQuery("Known@Example.com"), CancellationToken.None);
        var again = await _handler.Handle(new IdentifySignInQuery("Known@Example.com"), CancellationToken.None);
        var padded = await _handler.Handle(new IdentifySignInQuery("  known@example.com "), CancellationToken.None);

        again.Data.Should().Be(first.Data);
        padded.Data.Should().Be(first.Data);
    }
}
