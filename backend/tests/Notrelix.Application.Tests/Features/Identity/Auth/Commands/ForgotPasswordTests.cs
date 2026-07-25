using Notrelix.Application.Features.Identity.Auth.Commands.ForgotPassword;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class ForgotPasswordTests : IdentityHandlerTestBase
{
    private ForgotPasswordCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        OtpServiceMock.Object,
        RateLimitServiceMock.Object,
        EmailServiceMock.Object,
        ForgotPasswordLoggerMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsSuccess()
    {
        SetupUsers();
        RateLimitServiceMock.Setup(r => r.IsRateLimitedAsync("forgot-password", TestEmail.ToLowerInvariant(), 3, TimeSpan.FromHours(1)))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.Handle(new ForgotPasswordCommand { Email = TestEmail }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        EmailServiceMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExists_SendsEmail()
    {
        var user = CreateUser();
        SetupUsers(user);
        RateLimitServiceMock.Setup(r => r.IsRateLimitedAsync("forgot-password", TestEmail.ToLowerInvariant(), 3, TimeSpan.FromHours(1)))
            .ReturnsAsync(false);
        OtpServiceMock.Setup(o => o.GenerateAsync("forgot-password", TestEmail.ToLowerInvariant()))
            .ReturnsAsync("123456");

        var sut = CreateSut();
        var result = await sut.Handle(new ForgotPasswordCommand { Email = TestEmail }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        EmailServiceMock.Verify(e => e.SendAsync(TestEmail.ToLowerInvariant(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRateLimited_ReturnsFailure()
    {
        RateLimitServiceMock.Setup(r => r.IsRateLimitedAsync("forgot-password", TestEmail.ToLowerInvariant(), 3, TimeSpan.FromHours(1)))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.Handle(new ForgotPasswordCommand { Email = TestEmail }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Too many requests"));
    }
}
