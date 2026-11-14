using System.Reflection;
using Notrelix.Application.Features.Integrations.Connections.Queries.ListConnectionDeliveryLog;

namespace Notrelix.Application.Tests.Features.Integrations;

public class ConnectionDeliveryLogTests
{
    private static readonly string[] ForbiddenMembers =
    [
        "Payload", "Body", "ResponseBody", "Headers", "Secret", "SigningSecret", "Signature", "Token", "Digest", "Url", "TargetUrl",
    ];

    [Fact]
    public void DeliveryDto_HasNoPayloadBodyHeaderOrSecretMember()
    {
        var members = typeof(OutboundDeliveryLogEntryDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToList();

        members.Should().Equal(
            "Id", "EventType", "Status", "AttemptCount", "ResponseStatusCode", "FailureClass", "SkipReason",
            "CreatedAt", "UpdatedAt", "DeliveredAt");
        members.Should().NotContain(name => ForbiddenMembers.Any(name.Contains));
    }

    [Fact]
    public void Serialized_DeliveryDto_DoesNotCarryTheSecretOrThePayloadMarkers()
    {
        var dto = new OutboundDeliveryLogEntryDto(
            Guid.NewGuid(), "item.created", "Failed", 8, 500, "ReceiverError", null,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, null);

        var json = System.Text.Json.JsonSerializer.Serialize(dto);

        json.Should().NotContainAny("whsec_", "sha256=", "X-Notrelix", "payload", "responseBody", "signingSecret");
    }

    [Theory]
    [InlineData("not a cursor!!")]
    [InlineData("YWJj")]
    public void Validator_RefusesAMalformedCursor(string cursor)
    {
        var validator = new ListConnectionDeliveryLogQueryValidator();

        var result = validator.Validate(new ListConnectionDeliveryLogQuery(Guid.NewGuid(), Guid.NewGuid(), cursor));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validator_BoundsTheLimitToTwoHundred(int limit)
    {
        var validator = new ListConnectionDeliveryLogQueryValidator();

        validator.Validate(new ListConnectionDeliveryLogQuery(Guid.NewGuid(), Guid.NewGuid(), Limit: limit)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Defaults_AreFiftyAndTwoHundred()
    {
        ListConnectionDeliveryLogQuery.DefaultLimit.Should().Be(50);
        ListConnectionDeliveryLogQuery.MaxLimit.Should().Be(200);
        new ListConnectionDeliveryLogQueryValidator()
            .Validate(new ListConnectionDeliveryLogQuery(Guid.NewGuid(), Guid.NewGuid(), Limit: 200)).IsValid.Should().BeTrue();
    }
}
