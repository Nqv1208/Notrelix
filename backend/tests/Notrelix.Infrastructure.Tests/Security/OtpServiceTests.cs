using Notrelix.Infrastructure.Security.Otp;
using StackExchange.Redis;

namespace Notrelix.Infrastructure.Tests.Security;

public class OtpServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();

    private OtpService CreateService()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_dbMock.Object);
        return new OtpService(_redisMock.Object);
    }

    [Fact]
    public async Task GenerateAsync_SetsCodeWithTtlAndResetsAttempts()
    {
        var service = CreateService();
        _dbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(), It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var code = await service.GenerateAsync("forgot-password", "user@example.com");

        code.Should().MatchRegex(@"^\d{6}$");
        _dbMock.Verify(d => d.StringSetAsync(
            new RedisKey("Notrelix_otp:forgot-password:user@example.com"),
            new RedisValue(code),
            It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Once);
        _dbMock.Verify(d => d.KeyDeleteAsync(
            new RedisKey("Notrelix_otp:attempts:forgot-password:user@example.com"),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WhenCodeMatches_DeletesKeyAndAttempts()
    {
        var service = CreateService();
        var key = new RedisKey("Notrelix_otp:forgot-password:user@example.com");
        var attemptsKey = new RedisKey("Notrelix_otp:attempts:forgot-password:user@example.com");
        _dbMock.Setup(d => d.StringIncrementAsync(attemptsKey, It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        _dbMock.Setup(d => d.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("123456"));

        var result = await service.ValidateAsync("forgot-password", "user@example.com", "123456");

        result.Should().BeTrue();
        _dbMock.Verify(d => d.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Once);
        _dbMock.Verify(d => d.KeyDeleteAsync(attemptsKey, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WhenCodeMatches_SecondAttemptFailsBecauseCodeWasDeleted()
    {
        var service = CreateService();
        var key = new RedisKey("Notrelix_otp:forgot-password:user@example.com");
        var attemptsKey = new RedisKey("Notrelix_otp:attempts:forgot-password:user@example.com");
        _dbMock.Setup(d => d.StringIncrementAsync(attemptsKey, It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        _dbMock.Setup(d => d.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var result = await service.ValidateAsync("forgot-password", "user@example.com", "123456");

        result.Should().BeFalse();
        _dbMock.Verify(d => d.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_WhenWrongCode_FailsWithoutDeletingCode()
    {
        var service = CreateService();
        var key = new RedisKey("Notrelix_otp:forgot-password:user@example.com");
        _dbMock.Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        _dbMock.Setup(d => d.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("111111"));

        var result = await service.ValidateAsync("forgot-password", "user@example.com", "999999");

        result.Should().BeFalse();
        _dbMock.Verify(d => d.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_WhenAttemptsExceedMax_DeletesCodeAndReturnsFalse()
    {
        var service = CreateService();
        var key = new RedisKey("Notrelix_otp:forgot-password:user@example.com");
        var attemptsKey = new RedisKey("Notrelix_otp:attempts:forgot-password:user@example.com");
        _dbMock.Setup(d => d.StringIncrementAsync(attemptsKey, It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(6);
        _dbMock.Setup(d => d.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var result = await service.ValidateAsync("forgot-password", "user@example.com", "123456");

        result.Should().BeFalse();
        _dbMock.Verify(d => d.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Once);
        _dbMock.Verify(d => d.StringGetAsync(key, It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task GetAttemptsAsync_WhenNoAttempts_ReturnsZero()
    {
        var service = CreateService();
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var result = await service.GetAttemptsAsync("forgot-password", "user@example.com");

        result.Should().Be(0);
    }
}
