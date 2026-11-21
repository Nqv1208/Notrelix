using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Oidc;
using Notrelix.Application.Features.Accounts.Sso;
using Notrelix.Application.Features.Accounts.Sso.Commands.AddSsoDomain;
using Notrelix.Application.Features.Accounts.Sso.Commands.RemoveSsoDomain;
using Notrelix.Application.Features.Accounts.Sso.Commands.RemoveSsoProvider;
using Notrelix.Application.Features.Accounts.Sso.Commands.SaveSsoProvider;
using Notrelix.Application.Features.Accounts.Sso.Commands.SetSsoRequired;
using Notrelix.Application.Features.Accounts.Sso.Commands.VerifySsoDomain;
using Notrelix.Application.Features.Accounts.Sso.Queries.GetAccountSso;
using Notrelix.Domain.Accounts.Domains;

namespace Notrelix.Application.Tests.Features.Accounts.Sso;

public sealed class AccountSsoAdminTests
{
    private const string Issuer = "https://login.acme.example";
    private const string PlainSecret = "super-secret-client-value-123";

    private readonly AccountSsoTestKit _k = new();

    private AddSsoDomainCommandHandler Add() => new(_k.Context, _k.Request, _k.Clock, _k.Encryptor, _k.Claims);

    private VerifySsoDomainCommandHandler Verify() => new(_k.Context, _k.Request, _k.Clock, _k.Encryptor, _k.Dns, _k.Claims);

    private SaveSsoProviderCommandHandler Save() => new(_k.Context, _k.Request, _k.Clock, _k.Encryptor, _k.Discovery);

    private SetSsoRequiredCommandHandler Require() => new(_k.Context, _k.Request, _k.Clock);

    private async Task<SsoDomainDto> AddDomainAsync(string domain)
    {
        var result = await Add().Handle(new AddSsoDomainCommand(domain), default);
        await _k.Context.SaveChangesAsync();
        return result.Data!;
    }

    // ── domains ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddDomain_NormalizesAndReturnsPendingRow_WithTheTxtInstructions_AndStoresTheTokenProtected()
    {
        var row = await AddDomainAsync("  Acme.COM. ");

        row.Domain.Should().Be("acme.com");
        row.Status.Should().Be("Pending");
        row.TxtHost.Should().Be("acme.com");
        row.TxtValue.Should().StartWith("notrelix-verify=").And.HaveLength("notrelix-verify=".Length + 43);
        row.Claimed.Should().BeFalse();

        var stored = await _k.Context.AccountDomains.SingleAsync();
        var token = row.TxtValue!["notrelix-verify=".Length..];
        stored.VerificationTokenProtected.Should().NotBeNull().And.NotContain(token);
        stored.VerificationTokenHash.Should().Be(AccountSsoSupport.HashToken(token));
    }

    [Fact]
    public async Task AddDomain_Twice_ReturnsTheExistingRow_WithTheSameInstructions()
    {
        var first = await AddDomainAsync("acme.com");
        var second = await AddDomainAsync("ACME.com");

        second.Id.Should().Be(first.Id);
        second.TxtValue.Should().Be(first.TxtValue);
        (await _k.Context.AccountDomains.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData("gmail.com")]
    [InlineData("Outlook.com")]
    [InlineData("proton.me")]
    [InlineData("163.com")]
    public async Task AddDomain_PublicMailProvider_IsRefused(string domain)
    {
        var act = () => Add().Handle(new AddSsoDomainCommand(domain), default);

        (await act.Should().ThrowAsync<UnprocessableException>()).Which.ErrorCode.Should().Be("accounts.domain.public-provider");
    }

    [Theory]
    [InlineData("not a domain")]
    [InlineData("localhost")]
    [InlineData("https://acme.com")]
    [InlineData("a@acme.com")]
    [InlineData("-bad.acme.com")]
    [InlineData("10.0.0.1")]
    [InlineData("acme..com")]
    [InlineData("münchen.de")]
    public async Task AddDomain_Malformed_IsRefused(string domain)
    {
        var act = () => Add().Handle(new AddSsoDomainCommand(domain), default);

        (await act.Should().ThrowAsync<UnprocessableException>()).Which.ErrorCode.Should().Be("accounts.domain.invalid");
    }

    [Fact]
    public async Task AddDomain_VerifiedByAnotherAccount_IsRefusedAsClaimed()
    {
        _k.AddDomain(_k.OtherAccountId, "acme.com", verified: true);

        var act = () => Add().Handle(new AddSsoDomainCommand("acme.com"), default);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("accounts.domain.claimed");
    }

    [Fact]
    public async Task AddDomain_PendingAtAnotherAccount_DoesNotBlock_NoSquatting()
    {
        _k.AddDomain(_k.OtherAccountId, "acme.com", verified: false);

        var row = await AddDomainAsync("acme.com");

        row.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task VerifyDomain_WhenTheTxtRecordMatches_MakesItVerified_AndStopsShowingInstructions()
    {
        var added = await AddDomainAsync("acme.com");
        _k.Dns.Publish("acme.com", added.TxtValue!);

        var result = await Verify().Handle(new VerifySsoDomainCommand(added.Id), default);

        result.Data!.Status.Should().Be("Verified");
        result.Data.TxtValue.Should().BeNull();
        result.Data.VerifiedAt.Should().Be(AccountSsoTestKit.Now);
        (await _k.Context.AccountDomains.SingleAsync()).VerificationTokenProtected.Should().BeNull();
    }

    [Fact]
    public async Task VerifyDomain_WhenTheRecordIsQuotedLikeDnsToolsShowIt_StillMatches()
    {
        var added = await AddDomainAsync("acme.com");
        _k.Dns.Publish("acme.com", "\"" + added.TxtValue + "\"");

        (await Verify().Handle(new VerifySsoDomainCommand(added.Id), default)).Data!.Status.Should().Be("Verified");
    }

    [Fact]
    public async Task VerifyDomain_WhenNoRecordMatches_StaysPending_NeverAnError()
    {
        var added = await AddDomainAsync("acme.com");
        _k.Dns.Publish("acme.com", "notrelix-verify=some-other-token");
        _k.Dns.Publish("acme.com", "v=spf1 -all");

        var result = await Verify().Handle(new VerifySsoDomainCommand(added.Id), default);

        result.Succeeded.Should().BeTrue();
        result.Data!.Status.Should().Be("Pending");
        result.Data.TxtValue.Should().Be(added.TxtValue);
    }

    [Fact]
    public async Task VerifyDomain_WhenDnsIsDown_StaysPending_NeverAnError()
    {
        var added = await AddDomainAsync("acme.com");
        _k.Dns.Publish("acme.com", added.TxtValue!);
        _k.Dns.Unavailable = true;

        var result = await Verify().Handle(new VerifySsoDomainCommand(added.Id), default);

        result.Succeeded.Should().BeTrue();
        result.Data!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task VerifyDomain_WhenAnotherAccountVerifiedItFirst_IsRefusedAsClaimed_WithoutLookingUpDns()
    {
        var added = await AddDomainAsync("acme.com");
        _k.AddDomain(_k.OtherAccountId, "acme.com", verified: true);
        _k.Dns.Publish("acme.com", added.TxtValue!);

        var act = () => Verify().Handle(new VerifySsoDomainCommand(added.Id), default);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("accounts.domain.claimed");
        _k.Dns.Lookups.Should().Be(0);
        (await _k.Context.AccountDomains.SingleAsync(d => d.AccountId == _k.AccountId)).VerificationStatus.Should().Be(DomainVerificationStatus.Pending);
    }

    [Fact]
    public async Task VerifyDomain_OfAnotherAccountsRow_IsNotFound()
    {
        var foreign = _k.AddDomain(_k.OtherAccountId, "other.com", verified: false);

        var act = () => Verify().Handle(new VerifySsoDomainCommand(foreign.Id), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task VerifyDomain_AlreadyVerified_IsANoOp()
    {
        var verified = _k.AddDomain(_k.AccountId, "acme.com", verified: true);

        var result = await Verify().Handle(new VerifySsoDomainCommand(verified.Id), default);

        result.Data!.Status.Should().Be("Verified");
        _k.Dns.Lookups.Should().Be(0);
    }

    // ── provider ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveProvider_WithAGoodDiscoveryDocument_IsActive_AndTheSecretIsStoredAsCiphertext()
    {
        _k.Discovery.Serve(Issuer);

        var result = await Save().Handle(new SaveSsoProviderCommand("Acme SSO", Issuer, "client-1", PlainSecret, true), default);
        await _k.Context.SaveChangesAsync();

        result.Data!.Status.Should().Be("Active");
        result.Data.ValidationError.Should().BeNull();
        var stored = await _k.Context.AccountIdentityProviders.SingleAsync();
        stored.ClientSecretProtected.Should().NotBeNullOrEmpty().And.NotContain(PlainSecret);
        _k.Encryptor.Unprotect(stored.ClientSecretProtected!, AccountSsoSupport.ClientSecretPurpose).Should().Be(PlainSecret);
        stored.SsoUrl.Should().Be(Issuer + "/authorize");
    }

    [Fact]
    public async Task SaveProvider_WithARefusedDocument_StaysDraft_WithTheStatedReason()
    {
        _k.Discovery.Refuse(Issuer, OidcDiscoveryFailure.IssuerMismatch);

        var result = await Save().Handle(new SaveSsoProviderCommand("Acme SSO", Issuer, "client-1", PlainSecret, true), default);

        result.Data!.Status.Should().Be("Draft");
        result.Data.ValidationError.Should().Be("issuer-mismatch");
        (await _k.Context.AccountIdentityProviders.CountAsync()).Should().Be(0, "nothing is saved until SaveChanges, and the row is Draft");
    }

    [Fact]
    public async Task SaveProvider_Twice_ReplacesTheConfiguration_AndKeepsTheStoredSecretWhenNoneIsGiven()
    {
        _k.Discovery.Serve(Issuer);
        await Save().Handle(new SaveSsoProviderCommand("Acme SSO", Issuer, "client-1", PlainSecret, true), default);
        await _k.Context.SaveChangesAsync();
        var before = (await _k.Context.AccountIdentityProviders.SingleAsync()).ClientSecretProtected;

        var result = await Save().Handle(new SaveSsoProviderCommand("Renamed", Issuer, "client-2", null, false), default);
        await _k.Context.SaveChangesAsync();

        result.Data!.Status.Should().Be("Active");
        var stored = await _k.Context.AccountIdentityProviders.SingleAsync();
        stored.Name.Should().Be("Renamed");
        stored.ClientId.Should().Be("client-2");
        stored.JitProvisioningEnabled.Should().BeFalse();
        stored.ClientSecretProtected.Should().Be(before);
    }

    [Fact]
    public async Task SaveProvider_FirstTimeWithoutASecret_IsRefused()
    {
        var act = () => Save().Handle(new SaveSsoProviderCommand("Acme SSO", Issuer, "client-1", "  ", true), default);

        (await act.Should().ThrowAsync<UnprocessableException>()).Which.ErrorCode.Should().Be("accounts.sso.client-secret-required");
    }

    [Theory]
    [InlineData("http://login.acme.example")]
    [InlineData("login.acme.example")]
    [InlineData("https://user:pw@login.acme.example")]
    [InlineData("https://login.acme.example/?x=1")]
    [InlineData("https://login.acme.example/#frag")]
    public async Task SaveProvider_MalformedIssuer_IsRefused(string issuer)
    {
        var act = () => Save().Handle(new SaveSsoProviderCommand("Acme SSO", issuer, "client-1", PlainSecret, true), default);

        (await act.Should().ThrowAsync<UnprocessableException>()).Which.ErrorCode.Should().Be("accounts.sso.invalid-issuer");
        _k.Discovery.Calls.Should().Be(0, "a malformed issuer is never fetched");
    }

    [Fact]
    public async Task SaveProvider_WhileSsoIsRequired_CannotBeReplacedByOneThatDoesNotValidate()
    {
        _k.AddProvider(_k.AccountId);
        _k.AddDomain(_k.AccountId, "acme.com", verified: true);
        _k.SetRequired(_k.AccountId, true);
        _k.Discovery.Refuse(Issuer, OidcDiscoveryFailure.Unreachable);

        var act = () => Save().Handle(new SaveSsoProviderCommand("Acme SSO", Issuer, "client-1", null, true), default);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("accounts.sso.required-in-use");
        (await _k.Context.AccountIdentityProviders.SingleAsync()).Status.Should().Be("Active");
    }

    [Fact]
    public void SaveProvider_RequestToString_NeverCarriesTheSecret()
    {
        new SaveSsoProviderCommand("n", Issuer, "client-1", PlainSecret, true).ToString().Should().NotContain(PlainSecret);
    }

    [Fact]
    public async Task RemoveProvider_RemovesIt_ButNotWhileSsoIsRequired()
    {
        _k.AddProvider(_k.AccountId);
        var handler = new RemoveSsoProviderCommandHandler(_k.Context, _k.Request);

        _k.AddDomain(_k.AccountId, "acme.com", verified: true);
        _k.SetRequired(_k.AccountId, true);
        var blocked = () => handler.Handle(new RemoveSsoProviderCommand(), default);
        (await blocked.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("accounts.sso.required-in-use");

        var setting = await _k.Context.AccountSettingsEntities.SingleAsync();
        setting.SettingValue = "false";
        await _k.Context.SaveChangesAsync();
        (await handler.Handle(new RemoveSsoProviderCommand(), default)).Succeeded.Should().BeTrue();
        await _k.Context.SaveChangesAsync();
        (await _k.Context.AccountIdentityProviders.CountAsync()).Should().Be(0);
    }

    // ── require ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Require_NeedsAnActiveProviderAndAVerifiedDomain()
    {
        var act = () => Require().Handle(new SetSsoRequiredCommand(true), default);
        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("accounts.sso.not-ready");

        _k.AddProvider(_k.AccountId, active: false);
        _k.AddDomain(_k.AccountId, "acme.com", verified: true);
        (await act.Should().ThrowAsync<ConflictException>("a Draft provider does not count")).Which.ErrorCode.Should().Be("accounts.sso.not-ready");
    }

    [Fact]
    public async Task Require_WithAProviderButOnlyAPendingDomain_IsNotReady()
    {
        _k.AddProvider(_k.AccountId);
        _k.AddDomain(_k.AccountId, "acme.com", verified: false);

        var act = () => Require().Handle(new SetSsoRequiredCommand(true), default);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("accounts.sso.not-ready");
    }

    [Fact]
    public async Task Require_WhenReady_TurnsOn_AndOffAgain_OnTheSameRow()
    {
        _k.AddProvider(_k.AccountId);
        _k.AddDomain(_k.AccountId, "acme.com", verified: true);

        (await Require().Handle(new SetSsoRequiredCommand(true), default)).Succeeded.Should().BeTrue();
        await _k.Context.SaveChangesAsync();
        (await _k.Context.AccountSettingsEntities.SingleAsync()).SettingValue.Should().Be("true");

        (await Require().Handle(new SetSsoRequiredCommand(false), default)).Succeeded.Should().BeTrue();
        await _k.Context.SaveChangesAsync();
        (await _k.Context.AccountSettingsEntities.SingleAsync()).SettingValue.Should().Be("false");
    }

    [Fact]
    public async Task Require_TurningOff_NeedsNothing()
    {
        (await Require().Handle(new SetSsoRequiredCommand(false), default)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveDomain_TheLastVerifiedOne_IsRefusedWhileRequired_ButASecondOneMayGo()
    {
        var first = _k.AddDomain(_k.AccountId, "acme.com", verified: true);
        var second = _k.AddDomain(_k.AccountId, "acme.org", verified: true);
        var pending = _k.AddDomain(_k.AccountId, "acme.net", verified: false);
        _k.AddProvider(_k.AccountId);
        _k.SetRequired(_k.AccountId, true);
        var handler = new RemoveSsoDomainCommandHandler(_k.Context, _k.Request);

        (await handler.Handle(new RemoveSsoDomainCommand(second.Id), default)).Succeeded.Should().BeTrue("another verified domain remains");
        await _k.Context.SaveChangesAsync();
        (await handler.Handle(new RemoveSsoDomainCommand(pending.Id), default)).Succeeded.Should().BeTrue("a pending claim is free to go");
        await _k.Context.SaveChangesAsync();

        var act = () => handler.Handle(new RemoveSsoDomainCommand(first.Id), default);
        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("accounts.sso.required-in-use");
    }

    [Fact]
    public async Task RemoveDomain_OfAnotherAccount_RemovesNothing()
    {
        var foreign = _k.AddDomain(_k.OtherAccountId, "other.com", verified: true);
        var handler = new RemoveSsoDomainCommandHandler(_k.Context, _k.Request);

        (await handler.Handle(new RemoveSsoDomainCommand(foreign.Id), default)).Succeeded.Should().BeTrue();
        await _k.Context.SaveChangesAsync();

        (await _k.Context.AccountDomains.CountAsync()).Should().Be(1);
    }

    // ── the read ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Read_ShowsTheSetup_NeverTheSecret_AndInstructionsOnlyForPendingDomains()
    {
        _k.Discovery.Serve(Issuer);
        await Save().Handle(new SaveSsoProviderCommand("Acme SSO", Issuer, "client-1", PlainSecret, true), default);
        await _k.Context.SaveChangesAsync();
        var pending = await AddDomainAsync("acme.org");
        _k.AddDomain(_k.AccountId, "acme.com", verified: true);
        _k.AddDomain(_k.OtherAccountId, "acme.org", verified: true);
        var manage = new Mock<Notrelix.Application.Features.Accounts.Security.IAccountSecurityManageAuthorizer>();
        manage.Setup(m => m.CanManageAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = new GetAccountSsoQueryHandler(_k.Context, _k.Request, manage.Object, _k.Encryptor, _k.Claims);

        var dto = (await sut.Handle(new GetAccountSsoQuery(), default)).Data!;

        dto.CanManage.Should().BeTrue();
        dto.RequireSso.Should().BeFalse();
        dto.Provider.Should().BeEquivalentTo(new SsoProviderDto("Acme SSO", Issuer, "client-1", true, "Active", null, true));
        dto.Domains.Select(d => d.Domain).Should().Equal("acme.com", "acme.org");
        dto.Domains.Single(d => d.Domain == "acme.com").TxtValue.Should().BeNull();
        var org = dto.Domains.Single(d => d.Domain == "acme.org");
        org.TxtValue.Should().Be(pending.TxtValue);
        org.Claimed.Should().BeTrue("another account verified it");

        var json = JsonSerializer.Serialize(dto);
        json.Should().NotContain(PlainSecret).And.NotContain("enc:").And.NotContain("ecret\":\"");
        json.ToLowerInvariant().Should().NotContain("clientsecret\":\"");
    }

    // ── validators ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_RejectsBlankFields_AndAcceptsAMissingSecretForAnUpdate()
    {
        var v = new SaveSsoProviderCommandValidator();

        v.Validate(new SaveSsoProviderCommand("", Issuer, "c", "s", true)).IsValid.Should().BeFalse();
        v.Validate(new SaveSsoProviderCommand("n", "", "c", "s", true)).IsValid.Should().BeFalse();
        v.Validate(new SaveSsoProviderCommand("n", Issuer, "", "s", true)).IsValid.Should().BeFalse();
        v.Validate(new SaveSsoProviderCommand("n", Issuer, "c", null, true)).IsValid.Should().BeTrue();
    }
}
