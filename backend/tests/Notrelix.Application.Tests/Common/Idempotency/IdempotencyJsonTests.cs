using System.Text.Json;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Tests.Common.Idempotency;

/// <summary>
/// Spec 3.7: replayable responses must round-trip through the idempotency
/// serialization contract. Result envelopes have internal constructors and
/// getter-only properties — the default serializer cannot restore them, so the
/// replay options carry a dedicated converter.
/// </summary>
public class IdempotencyJsonTests
{
    [Fact]
    public void ResultOfT_Success_RoundTrips()
    {
        var original = Result<Guid>.Success(Guid.NewGuid());

        var json = JsonSerializer.Serialize(original, IdempotencyJson.Options);
        var restored = JsonSerializer.Deserialize<Result<Guid>>(json, IdempotencyJson.Options);

        restored.Should().NotBeNull();
        restored!.Succeeded.Should().BeTrue();
        restored.Data.Should().Be(original.Data);
        restored.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ResultOfT_Failure_RoundTrips()
    {
        var original = Result<Guid>.Failure("Board is archived");

        var json = JsonSerializer.Serialize(original, IdempotencyJson.Options);
        var restored = JsonSerializer.Deserialize<Result<Guid>>(json, IdempotencyJson.Options);

        restored.Should().NotBeNull();
        restored!.Succeeded.Should().BeFalse();
        restored.Data.Should().Be(Guid.Empty);
        restored.Errors.Should().Equal("Board is archived");
    }

    [Fact]
    public void ResultOfT_TypedErrors_RoundTrip()
    {
        var error = new ApplicationError(
            "board_archived", "Board is archived", ApplicationErrorType.BusinessRule, "boardId");
        var original = Result<int>.Failure(error);

        var json = JsonSerializer.Serialize(original, IdempotencyJson.Options);
        var restored = JsonSerializer.Deserialize<Result<int>>(json, IdempotencyJson.Options);

        restored.Should().NotBeNull();
        restored!.Succeeded.Should().BeFalse();
        restored.TypedErrors.Should().HaveCount(1);
        restored.TypedErrors[0].Should().Be(error);
        restored.Errors.Should().Equal("Board is archived");
    }

    [Fact]
    public void Result_NonGeneric_RoundTrips()
    {
        var original = Result.Failure("Not allowed");

        var json = JsonSerializer.Serialize(original, IdempotencyJson.Options);
        var restored = JsonSerializer.Deserialize<Result>(json, IdempotencyJson.Options);

        restored.Should().NotBeNull();
        restored!.Succeeded.Should().BeFalse();
        restored.Errors.Should().Equal("Not allowed");
    }

    [Fact]
    public void Record_Response_RoundTrips_With_CamelCase()
    {
        var original = new SampleDto(Guid.NewGuid(), "sample");

        var json = JsonSerializer.Serialize(original, IdempotencyJson.Options);
        json.Should().Contain("\"id\"").And.Contain("\"name\"");

        var restored = JsonSerializer.Deserialize<SampleDto>(json, IdempotencyJson.Options);
        restored.Should().Be(original);
    }

    private sealed record SampleDto(Guid Id, string Name);
}
