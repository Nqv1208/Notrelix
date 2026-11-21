using Notrelix.Application.Features.Identity.Auth.Queries.IdentifySignIn;
using Notrelix.Application.Tests.Features.Accounts.Sso;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Queries;

public class IdentifySignInTests
{
    private readonly IdentifySignInQueryHandler _handler = new(new Notrelix.Testing.Application.Fakes.FakeAccountSsoFacts());

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

    // ── routing by a verified domain (the real facts over the Accounts data) ─────

    private static async Task<SignInMethodDto> IdentifyAsync(AccountSsoTestKit kit, string email)
    {
        var result = await new IdentifySignInQueryHandler(kit.Facts).Handle(new IdentifySignInQuery(email), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        return result.Data!;
    }

    [Fact]
    public async Task AVerifiedDomainWithAnActiveProvider_ContinuesWithTheCompanyProvider_AndSaysWhetherItIsEnforced()
    {
        var kit = new AccountSsoTestKit();
        var account = kit.AddAccount(Guid.CreateVersion7(), "Globex-Corp");
        kit.AddDomain(account.Id, "globex.com", verified: true);
        kit.AddProvider(account.Id);

        (await IdentifyAsync(kit, "Someone@Globex.com")).Should().Be(
            new SignInMethodDto(SignInMethods.Sso, new SsoSignInDto("Globex-Corp", "GC", "oidc", "Acme SSO", false)));

        kit.SetRequired(account.Id, true);
        (await IdentifyAsync(kit, "someone@globex.com")).Sso!.Enforced.Should().BeTrue();
    }

    [Fact]
    public async Task AnUnverifiedDomain_AnUnclaimedOne_AndAnAccountWithoutAnActiveProvider_AllAnswerPassword_WithTheSameShape()
    {
        var kit = new AccountSsoTestKit();
        var pending = kit.AddAccount(Guid.CreateVersion7(), "Pending-Co");
        kit.AddDomain(pending.Id, "pending.example", verified: false);
        kit.AddProvider(pending.Id);
        var draft = kit.AddAccount(Guid.CreateVersion7(), "Draft-Co");
        kit.AddDomain(draft.Id, "draft.example", verified: true);
        kit.AddProvider(draft.Id, active: false);

        var expected = new SignInMethodDto(SignInMethods.Password, null);
        (await IdentifyAsync(kit, "x@pending.example")).Should().Be(expected, "an unverified domain does not route");
        (await IdentifyAsync(kit, "x@draft.example")).Should().Be(expected, "a domain without an Active provider does not route");
        (await IdentifyAsync(kit, "x@nobody-claimed-this.example")).Should().Be(expected);
    }
}
