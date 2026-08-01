using FluentAssertions;
using Notrelix.Domain.Accounts.Scim;

namespace Notrelix.Domain.Tests.Accounts;

public class ScimDirectoryTests
{
    private readonly Guid _accountId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var idpId = Guid.NewGuid();
        var directory = ScimDirectory.Create(_accountId, "Okta SCIM", idpId, "https://scim.example.com");

        directory.AccountId.Should().Be(_accountId);
        directory.Name.Should().Be("Okta SCIM");
        directory.IdentityProviderId.Should().Be(idpId);
        directory.BaseUrl.Should().Be("https://scim.example.com");
        directory.Status.Should().Be("Active");
        directory.LastSyncAt.Should().BeNull();
        directory.BearerTokenHash.Should().BeNull();
    }

    [Fact]
    public void Create_WithMinimalData_ShouldSucceed()
    {
        var directory = ScimDirectory.Create(_accountId, "Basic SCIM");

        directory.IdentityProviderId.Should().BeNull();
        directory.BaseUrl.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => ScimDirectory.Create(Guid.Empty, "SCIM");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => ScimDirectory.Create(_accountId, "  ");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Disable_ShouldSetStatusToDisabled()
    {
        var directory = ScimDirectory.Create(_accountId, "SCIM");

        directory.Disable();

        directory.Status.Should().Be("Disabled");
    }

    [Fact]
    public void Enable_WhenDisabled_ShouldSetStatusToActive()
    {
        var directory = ScimDirectory.Create(_accountId, "SCIM");
        directory.Disable();

        directory.Enable();

        directory.Status.Should().Be("Active");
    }

    [Fact]
    public void Enable_WhenAlreadyActive_ShouldBeIdempotent()
    {
        var directory = ScimDirectory.Create(_accountId, "SCIM");

        directory.Enable();

        directory.Status.Should().Be("Active");
    }

    [Fact]
    public void MarkError_ShouldSetStatusToError()
    {
        var directory = ScimDirectory.Create(_accountId, "SCIM");

        directory.MarkError();

        directory.Status.Should().Be("Error");
    }

    [Fact]
    public void RecordSync_ShouldUpdateLastSyncAt()
    {
        var directory = ScimDirectory.Create(_accountId, "SCIM");
        var syncedAt = DateTimeOffset.UtcNow;

        directory.RecordSync(syncedAt);

        directory.LastSyncAt.Should().Be(syncedAt);
    }

    [Fact]
    public void UpdateCredentials_ShouldUpdateBearerTokenHash()
    {
        var directory = ScimDirectory.Create(_accountId, "SCIM");

        directory.UpdateCredentials("hashed-token");

        directory.BearerTokenHash.Should().Be("hashed-token");
    }

    [Fact]
    public void UpdateCredentials_WithNull_ShouldClear()
    {
        var directory = ScimDirectory.Create(_accountId, "SCIM");
        directory.UpdateCredentials("hashed-token");

        directory.UpdateCredentials(null);

        directory.BearerTokenHash.Should().BeNull();
    }
}
