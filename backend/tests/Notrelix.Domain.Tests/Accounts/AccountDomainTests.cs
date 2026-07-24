using FluentAssertions;
using Notrelix.Domain.Accounts.Domains;

namespace Notrelix.Domain.Tests.Accounts;

public class AccountDomainTests
{
    private readonly Guid _accountId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var domain = AccountDomain.Create(_accountId, "Example.COM", "token-hash");

        domain.AccountId.Should().Be(_accountId);
        domain.Domain.Should().Be("example.com");
        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Pending);
        domain.VerificationTokenHash.Should().Be("token-hash");
        domain.AutoJoinEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => AccountDomain.Create(Guid.Empty, "example.com");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyDomain_ShouldThrow()
    {
        var act = () => AccountDomain.Create(_accountId, "  ");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Verify_ShouldChangeStatusToVerified()
    {
        var domain = AccountDomain.Create(_accountId, "example.com");
        var verifiedAt = DateTimeOffset.UtcNow;

        domain.Verify(verifiedAt);

        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Verified);
        domain.VerifiedAt.Should().Be(verifiedAt);
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_ShouldBeIdempotent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com");
        var firstVerifiedAt = DateTimeOffset.UtcNow;
        domain.Verify(firstVerifiedAt);

        var secondVerifiedAt = firstVerifiedAt.AddHours(1);
        domain.Verify(secondVerifiedAt);

        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Verified);
        domain.VerifiedAt.Should().Be(firstVerifiedAt);
    }

    [Fact]
    public void Reject_ShouldChangeStatusToRejected()
    {
        var domain = AccountDomain.Create(_accountId, "example.com");

        domain.Reject();

        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Rejected);
    }

    [Fact]
    public void EnableAutoJoin_WhenNotVerified_ShouldThrow()
    {
        var domain = AccountDomain.Create(_accountId, "example.com");

        var act = () => domain.EnableAutoJoin();

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*unverified*");
    }

    [Fact]
    public void EnableAutoJoin_WhenRejected_ShouldThrow()
    {
        var domain = AccountDomain.Create(_accountId, "example.com");
        domain.Reject();

        var act = () => domain.EnableAutoJoin();

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void EnableAutoJoin_WhenVerified_ShouldSucceed()
    {
        var domain = AccountDomain.Create(_accountId, "example.com");
        domain.Verify(DateTimeOffset.UtcNow);

        domain.EnableAutoJoin();

        domain.AutoJoinEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisableAutoJoin_ShouldSetToFalse()
    {
        var domain = AccountDomain.Create(_accountId, "example.com");
        domain.Verify(DateTimeOffset.UtcNow);
        domain.EnableAutoJoin();

        domain.DisableAutoJoin();

        domain.AutoJoinEnabled.Should().BeFalse();
    }
}
