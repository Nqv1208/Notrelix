using System.Text.Json;

namespace Notrelix.API.Tests.Assertions;

public static class ProblemDetailsAssertions
{
    public static void ShouldBeValidProblemDetails(this JsonElement json, int expectedStatus)
    {
        json.GetProperty("status").GetInt32().Should().Be(expectedStatus);
        json.TryGetProperty("type", out var type).Should().BeTrue("type is required per RFC 9457");
        type.GetString()!.Should().StartWith("https://", "type must be a URI per RFC 9457");
        json.TryGetProperty("title", out _).Should().BeTrue("title is required per RFC 9457");
        json.TryGetProperty("detail", out _).Should().BeTrue("detail is required per RFC 9457");
        json.TryGetProperty("instance", out _).Should().BeTrue("instance identifies the request URL");
        json.TryGetProperty("traceId", out _).Should().BeTrue("traceId is required for debugging");
    }

    public static async Task<JsonElement> ReadAsJsonElementAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(body);
    }
}
