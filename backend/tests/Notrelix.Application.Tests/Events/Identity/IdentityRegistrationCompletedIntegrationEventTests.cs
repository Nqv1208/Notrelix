using Notrelix.Application.Events.Identity;

namespace Notrelix.Application.Tests.Events.Identity;

public class IdentityRegistrationCompletedIntegrationEventTests
{
    [Fact]
    public void Constructor_RejectsNullAccountId()
    {
        var act = () => new IdentityRegistrationCompletedIntegrationEventV1(
            EventId: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            AccountId: null,
            Email: "user@example.com",
            DisplayName: "User",
            AccountName: "User's Account",
            CorrelationId: Guid.CreateVersion7());

        act.Should().Throw<ArgumentException>().WithParameterName("accountId");
    }

    [Fact]
    public void Constructor_RejectsEmptyAccountId()
    {
        var act = () => new IdentityRegistrationCompletedIntegrationEventV1(
            EventId: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            AccountId: Guid.Empty,
            Email: "user@example.com",
            DisplayName: "User",
            AccountName: "User's Account",
            CorrelationId: Guid.CreateVersion7());

        act.Should().Throw<ArgumentException>().WithParameterName("accountId");
    }
}
