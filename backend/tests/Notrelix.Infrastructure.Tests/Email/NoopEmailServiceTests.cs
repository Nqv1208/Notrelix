using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Email;
using Notrelix.Infrastructure.Email;

namespace Notrelix.Infrastructure.Tests.Email;

public sealed class NoopEmailServiceTests
{
    [Fact]
    public async Task SendAsync_SkipsDeliveryWithoutLoggingTheRecipientAddress()
    {
        var logger = new Mock<ILogger<NoopEmailService>>();
        var sut = new NoopEmailService(logger.Object);
        var request = new EmailDeliveryRequest("person@example.com", "Person", "Welcome", "<p>hi</p>", null, "key-1");

        var result = await sut.SendAsync(request);

        result.Provider.Should().Be("noop");
        logger.Invocations
            .Should().ContainSingle()
            .Which.Arguments
            .Select(a => a?.ToString() ?? string.Empty)
            .Should().NotContain(a => a.Contains("person@example.com", StringComparison.OrdinalIgnoreCase));
    }
}
