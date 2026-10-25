using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Email;
using Notrelix.Infrastructure.Email;

namespace Notrelix.Infrastructure.Tests.Email;

public sealed class NoopEmailServiceTests
{
    private static Mock<IHostEnvironment> Environment(string name)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(name);
        return environment;
    }

    [Fact]
    public async Task SendAsync_OutsideDevelopment_SkipsDeliveryWithoutLoggingTheRecipientAddress()
    {
        var logger = new Mock<ILogger<NoopEmailService>>();
        var sut = new NoopEmailService(logger.Object, Environment(Environments.Production).Object);
        var request = new EmailDeliveryRequest("person@example.com", "Person", "Welcome", "<p>hi</p>", null, "key-1");

        var result = await sut.SendAsync(request);

        result.Provider.Should().Be("noop");
        logger.Invocations
            .Should().ContainSingle()
            .Which.Arguments
            .Select(a => a?.ToString() ?? string.Empty)
            .Should().NotContain(a => a.Contains("person@example.com", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SendAsync_InDevelopment_LogsTheMessageTextSoTheLinkCanBeFollowed()
    {
        var logger = new Mock<ILogger<NoopEmailService>>();
        var sut = new NoopEmailService(logger.Object, Environment(Environments.Development).Object);
        var request = new EmailDeliveryRequest("person@example.com", "Person", "Welcome", "<p>hi</p>", "open the link", "key-2");

        var result = await sut.SendAsync(request);

        result.Provider.Should().Be("noop");
        logger.Invocations.Should().HaveCount(2);
        var arguments = logger.Invocations
            .SelectMany(i => i.Arguments)
            .Select(a => a?.ToString() ?? string.Empty)
            .ToList();
        arguments.Should().Contain(a => a.Contains("open the link", StringComparison.Ordinal));
    }
}
