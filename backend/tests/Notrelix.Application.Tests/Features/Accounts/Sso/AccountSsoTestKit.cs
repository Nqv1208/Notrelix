using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Accounts.Sso;
using Notrelix.Application.Features.Accounts.Members;
using Notrelix.Application.Tests.Features.Accounts.Members;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Accounts.Sso;

/// <summary>One Account (the acting Owner's) over EF InMemory with the fake ports; a second Account for cross-tenant cases.</summary>
public sealed class AccountSsoTestKit
{
    public static readonly DateTimeOffset Now = new(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);

    public TestAccountDbContext Context { get; }
    public FakeCurrentRequestContext Request { get; } = new();
    public FakeDateTimeProvider Clock { get; } = FakeDateTimeProvider.WithFixedTime(Now);
    public ReversibleSecretEncryptor Encryptor { get; } = new();
    public FakeDomainTxtResolver Dns { get; } = new();
    public FakeOidcDiscoveryClient Discovery { get; } = new();
    public Guid AccountId { get; } = Guid.CreateVersion7();
    public Guid OwnerId { get; } = Guid.CreateVersion7();
    public Guid OtherAccountId { get; } = Guid.CreateVersion7();

    public AccountSsoTestKit()
    {
        Context = new TestAccountDbContext(new DbContextOptionsBuilder<TestAccountDbContext>()
            .UseInMemoryDatabase($"sso-{Guid.NewGuid():N}").Options);
        Request.AsUser(OwnerId);
        Request.Tenant.SetAccount(AccountId, OwnerId);
        AddAccount(AccountId, "Acme");
        AddAccount(OtherAccountId, "Other");
    }

    public SsoDomainClaims Claims => new(Context);

    public AccountSsoFactsProvider Facts => new(Context, Encryptor, new Mock<IAccountGrantProjectionService>().Object);

    public Account AddAccount(Guid id, string name, bool closed = false)
    {
        var account = Account.Create(name, name.ToLowerInvariant(), AccountType.Team, OwnerId, Now);
        typeof(Account).GetProperty(nameof(Account.Id))!.SetValue(account, id);
        if (closed) account.Archive(OwnerId, Now);
        Context.Accounts.Add(account);
        Context.SaveChanges();
        return account;
    }

    public AccountDomain AddDomain(Guid accountId, string domain, bool verified, string token = "tok")
    {
        var row = AccountDomain.Create(
            accountId, domain, OwnerId, Now, AccountSsoSupport.HashToken(token),
            Encryptor.Protect(token, AccountSsoSupport.DomainTokenPurpose));
        if (verified) row.Verify(Now, OwnerId);
        Context.AccountDomains.Add(row);
        Context.SaveChanges();
        return row;
    }

    public AccountIdentityProvider AddProvider(Guid accountId, bool active = true, string secret = "s3cret-plain", bool jit = true)
    {
        var provider = AccountIdentityProvider.CreateOidc(
            accountId, "Acme SSO", "https://login.acme.example", "client-1",
            Encryptor.Protect(secret, AccountSsoSupport.ClientSecretPurpose), jit, OwnerId, Now);
        if (active) provider.MarkValidated("https://login.acme.example/authorize", OwnerId, Now);
        Context.AccountIdentityProviders.Add(provider);
        Context.SaveChanges();
        return provider;
    }

    public void SetRequired(Guid accountId, bool required)
    {
        Context.AccountSettingsEntities.Add(new Notrelix.Application.Features.Accounts.Abstractions.Records.AccountSettingRecord
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            SettingKey = Notrelix.Application.Features.Accounts.Security.AccountSecuritySettingKeys.RequireSso,
            SettingValue = required ? "true" : "false",
            CreatedAt = Now,
        });
        Context.SaveChanges();
    }

    public void AddMember(Guid accountId, Guid userId, AccountRole role)
    {
        Context.AccountMembers.Add(AccountMember.Create(accountId, userId, role, OwnerId, Now));
        Context.SaveChanges();
    }
}
