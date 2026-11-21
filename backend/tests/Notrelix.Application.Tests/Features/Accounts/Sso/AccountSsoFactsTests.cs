using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Accounts.Public.Sso;
using Notrelix.Application.Features.Accounts.Sso;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Tests.Features.Accounts.Sso;

public sealed class AccountSsoFactsTests
{
    private readonly AccountSsoTestKit _k = new();
    private readonly Guid _person = Guid.CreateVersion7();

    private void Ready(bool verified = true, bool activeProvider = true)
    {
        _k.AddDomain(_k.AccountId, "acme.com", verified);
        _k.AddProvider(_k.AccountId, activeProvider);
    }

    [Fact]
    public async Task Route_ForAVerifiedDomainOfAnAccountWithAnActiveProvider_NamesTheAccountAndProvider()
    {
        Ready();

        var route = await _k.Facts.FindRouteAsync("Someone@ACME.com", default);

        route.Should().BeEquivalentTo(new AccountSsoRoute(_k.AccountId, "Acme", "Acme SSO", false));
    }

    [Fact]
    public async Task Route_CarriesTheEnforcedFlag()
    {
        Ready();
        _k.SetRequired(_k.AccountId, true);

        (await _k.Facts.FindRouteAsync("a@acme.com", default))!.Enforced.Should().BeTrue();
    }

    [Fact]
    public async Task Route_IsTheSameNull_ForEveryReasonItCannotRoute()
    {
        // an unverified domain, a Draft provider, a closed account, an unknown domain, a subdomain, no domain at all
        var unverified = new AccountSsoTestKit();
        unverified.AddDomain(unverified.AccountId, "acme.com", verified: false);
        unverified.AddProvider(unverified.AccountId);
        var draft = new AccountSsoTestKit();
        draft.AddDomain(draft.AccountId, "acme.com", verified: true);
        draft.AddProvider(draft.AccountId, active: false);
        var noProvider = new AccountSsoTestKit();
        noProvider.AddDomain(noProvider.AccountId, "acme.com", verified: true);
        var closed = new AccountSsoTestKit();
        var closedId = Guid.CreateVersion7();
        closed.AddAccount(closedId, "Closed", closed: true);
        closed.AddDomain(closedId, "acme.com", verified: true);
        closed.AddProvider(closedId);
        Ready();

        var answers = new[]
        {
            await unverified.Facts.FindRouteAsync("a@acme.com", default),
            await draft.Facts.FindRouteAsync("a@acme.com", default),
            await noProvider.Facts.FindRouteAsync("a@acme.com", default),
            await closed.Facts.FindRouteAsync("a@acme.com", default),
            await _k.Facts.FindRouteAsync("a@unknown.example", default),
            await _k.Facts.FindRouteAsync("a@eng.acme.com", default),
            await _k.Facts.FindRouteAsync("not-an-email", default),
            await _k.Facts.FindRouteAsync("", default),
        };

        answers.Should().AllSatisfy(a => a.Should().BeNull());
    }

    [Fact]
    public async Task Route_IsAFunctionOfTheDomainOnly_SameAnswerForAKnownAndAnUnknownAddress()
    {
        Ready();

        var known = await _k.Facts.FindRouteAsync("real.person@acme.com", default);
        var unknown = await _k.Facts.FindRouteAsync("nobody.at.all@acme.com", default);

        known.Should().BeEquivalentTo(unknown);
        known.Should().NotBeNull();
    }

    [Fact]
    public async Task Provider_IsOnlyServedForAnActiveProvider_AndNeverCarriesTheSecret()
    {
        Ready();

        var provider = await _k.Facts.GetProviderAsync(_k.AccountId, default);

        provider.Should().BeEquivalentTo(new AccountSsoProvider("Acme SSO", "https://login.acme.example", "client-1", true));
        (await _k.Facts.GetProviderAsync(_k.OtherAccountId, default)).Should().BeNull();
    }

    [Fact]
    public async Task CallbackProvider_HandsTheDecryptedSecretToIdentity_OnlyForAnActiveProvider()
    {
        Ready();

        var provider = await _k.Facts.GetCallbackProviderAsync(_k.AccountId, default);

        provider!.ClientSecret.Should().Be(_k.DefaultSecret);
        provider.ToString().Should().NotContain(_k.DefaultSecret, "the secret never reaches a log line through ToString");

        var draft = new AccountSsoTestKit();
        draft.AddProvider(draft.AccountId, active: false);
        (await draft.Facts.GetCallbackProviderAsync(draft.AccountId, default)).Should().BeNull();
    }

    [Fact]
    public async Task EmailDomainVerified_IsTrueOnlyForThisAccountsVerifiedDomain()
    {
        Ready();
        _k.AddDomain(_k.AccountId, "pending.example", verified: false);
        _k.AddDomain(_k.OtherAccountId, "other.example", verified: true);

        (await _k.Facts.IsEmailDomainVerifiedAsync(_k.AccountId, "x@acme.com", default)).Should().BeTrue();
        (await _k.Facts.IsEmailDomainVerifiedAsync(_k.AccountId, "x@pending.example", default)).Should().BeFalse();
        (await _k.Facts.IsEmailDomainVerifiedAsync(_k.AccountId, "x@other.example", default)).Should().BeFalse();
        (await _k.Facts.IsEmailDomainVerifiedAsync(_k.OtherAccountId, "x@acme.com", default)).Should().BeFalse();
        (await _k.Facts.IsEmailDomainVerifiedAsync(_k.AccountId, "nonsense", default)).Should().BeFalse();
    }

    // ── enforcement ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AccountSsoSignInMethod.Password)]
    [InlineData(AccountSsoSignInMethod.Social)]
    public async Task Enforcement_WhenOff_HasNoEffect(AccountSsoSignInMethod method)
    {
        Ready();
        _k.SetRequired(_k.AccountId, false);

        (await _k.Facts.CheckEnforcementAsync("a@acme.com", _person, method, default)).Required.Should().BeFalse();
    }

    [Fact]
    public async Task Enforcement_WhenOn_RefusesPasswordAndSocialForAMemberOfAVerifiedDomain()
    {
        Ready();
        _k.SetRequired(_k.AccountId, true);
        _k.AddMember(_k.AccountId, _person, AccountRole.Member);

        var password = await _k.Facts.CheckEnforcementAsync("a@acme.com", _person, AccountSsoSignInMethod.Password, default);
        var social = await _k.Facts.CheckEnforcementAsync("a@acme.com", _person, AccountSsoSignInMethod.Social, default);

        password.Should().BeEquivalentTo(new AccountSsoEnforcement(true, _k.AccountId));
        social.Should().BeEquivalentTo(new AccountSsoEnforcement(true, _k.AccountId));
    }

    [Fact]
    public async Task Enforcement_WhenOn_ExemptsTheOwnerForPasswordOnly_NeverForSocial()
    {
        Ready();
        _k.SetRequired(_k.AccountId, true);
        _k.AddMember(_k.AccountId, _k.OwnerId, AccountRole.Owner);

        (await _k.Facts.CheckEnforcementAsync("owner@acme.com", _k.OwnerId, AccountSsoSignInMethod.Password, default))
            .Required.Should().BeFalse("the Owner keeps the password as the break-glass");
        (await _k.Facts.CheckEnforcementAsync("owner@acme.com", _k.OwnerId, AccountSsoSignInMethod.Social, default))
            .Required.Should().BeTrue("a social sign-in exempts nobody");
    }

    [Fact]
    public async Task Enforcement_AnAdminAndAnUnknownPersonAreNotExempt()
    {
        Ready();
        _k.SetRequired(_k.AccountId, true);
        _k.AddMember(_k.AccountId, _person, AccountRole.Admin);

        (await _k.Facts.CheckEnforcementAsync("a@acme.com", _person, AccountSsoSignInMethod.Password, default)).Required.Should().BeTrue();
        (await _k.Facts.CheckEnforcementAsync("a@acme.com", null, AccountSsoSignInMethod.Password, default)).Required.Should().BeTrue();
        (await _k.Facts.CheckEnforcementAsync("a@acme.com", Guid.CreateVersion7(), AccountSsoSignInMethod.Password, default)).Required.Should().BeTrue();
    }

    [Fact]
    public async Task Enforcement_OnlyAppliesToAVerifiedDomainOfTheEnforcingAccount()
    {
        Ready();
        _k.SetRequired(_k.AccountId, true);

        (await _k.Facts.CheckEnforcementAsync("a@other.example", _person, AccountSsoSignInMethod.Social, default)).Required.Should().BeFalse();
        (await _k.Facts.CheckEnforcementAsync("a@eng.acme.com", _person, AccountSsoSignInMethod.Social, default)).Required.Should().BeFalse();
        (await _k.Facts.CheckEnforcementAsync("garbage", _person, AccountSsoSignInMethod.Social, default)).Required.Should().BeFalse();

        var pending = new AccountSsoTestKit();
        pending.AddDomain(pending.AccountId, "acme.com", verified: false);
        pending.SetRequired(pending.AccountId, true);
        (await pending.Facts.CheckEnforcementAsync("a@acme.com", _person, AccountSsoSignInMethod.Password, default))
            .Required.Should().BeFalse("an unverified domain enforces nothing");
    }

    // ── join ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Join_AddsAnAccountMemberWithRoleMember_AndNothingElse()
    {
        Ready();

        (await _k.Facts.JoinAsMemberAsync(_k.AccountId, _person, "new@acme.com", AccountSsoTestKit.Now, default)).Should().BeTrue();
        await _k.Context.SaveChangesAsync();

        var member = await _k.Context.AccountMembers.SingleAsync();
        member.UserId.Should().Be(_person);
        member.Role.Should().Be(AccountRole.Member);
        member.Status.Should().Be(AccountMemberStatus.Active);
        (await _k.Context.Accounts.CountAsync()).Should().Be(2, "no personal Account is created");
    }

    [Fact]
    public async Task Join_IsRefused_ForAnEmailOutsideTheVerifiedDomains_AndForAnAccountWithoutAnActiveProvider()
    {
        Ready();
        (await _k.Facts.JoinAsMemberAsync(_k.AccountId, _person, "new@elsewhere.example", AccountSsoTestKit.Now, default)).Should().BeFalse();
        (await _k.Facts.JoinAsMemberAsync(_k.OtherAccountId, _person, "new@acme.com", AccountSsoTestKit.Now, default)).Should().BeFalse();

        var draft = new AccountSsoTestKit();
        draft.AddDomain(draft.AccountId, "acme.com", verified: true);
        draft.AddProvider(draft.AccountId, active: false);
        (await draft.Facts.JoinAsMemberAsync(draft.AccountId, _person, "new@acme.com", AccountSsoTestKit.Now, default)).Should().BeFalse();
        (await _k.Context.AccountMembers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Join_IsIdempotent_AndNeverReactivatesAnEndedMembership()
    {
        Ready();
        (await _k.Facts.JoinAsMemberAsync(_k.AccountId, _person, "a@acme.com", AccountSsoTestKit.Now, default)).Should().BeTrue();
        await _k.Context.SaveChangesAsync();
        (await _k.Facts.JoinAsMemberAsync(_k.AccountId, _person, "a@acme.com", AccountSsoTestKit.Now, default)).Should().BeTrue();
        (await _k.Context.AccountMembers.CountAsync()).Should().Be(1);

        var member = await _k.Context.AccountMembers.SingleAsync();
        member.Suspend(_k.OwnerId, AccountSsoTestKit.Now.AddMinutes(1), activeOwnerCount: 2);
        await _k.Context.SaveChangesAsync();

        (await _k.Facts.JoinAsMemberAsync(_k.AccountId, _person, "a@acme.com", AccountSsoTestKit.Now, default)).Should().BeFalse();
        (await _k.Context.AccountMembers.SingleAsync()).Status.Should().Be(AccountMemberStatus.Suspended);
    }

    [Fact]
    public async Task Join_NeverGivesAnotherRoleToAnExistingOwner()
    {
        Ready();
        _k.AddMember(_k.AccountId, _person, AccountRole.Owner);

        (await _k.Facts.JoinAsMemberAsync(_k.AccountId, _person, "a@acme.com", AccountSsoTestKit.Now, default)).Should().BeTrue();

        (await _k.Context.AccountMembers.SingleAsync()).Role.Should().Be(AccountRole.Owner);
    }

    // ── support ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("a@acme.com", "acme.com")]
    [InlineData("A@Acme.COM.", "acme.com")]
    [InlineData("x@y@acme.com", "acme.com")]
    [InlineData("noat", null)]
    [InlineData("@acme.com", null)]
    [InlineData("a@", null)]
    public void DomainOfEmail_TakesTheLastPart(string email, string? expected) =>
        AccountSsoSupport.DomainOfEmail(email).Should().Be(expected);

    [Theory]
    [InlineData("notrelix-verify=abc", "abc", true)]
    [InlineData("  \"notrelix-verify=abc\"  ", "abc", true)]
    [InlineData("notrelix-verify=abcd", "abc", false)]
    [InlineData("Notrelix-Verify=abc", "abc", false)]
    [InlineData("v=spf1 notrelix-verify=abc", "abc", false)]
    public void HasTxtValue_MatchesTheWholeRecordExactly(string record, string token, bool expected) =>
        AccountSsoSupport.HasTxtValue([record], token).Should().Be(expected);
}
