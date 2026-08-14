using System.Globalization;
using Notrelix.Infrastructure.Auth.Jwt;
using StackExchange.Redis;

namespace Notrelix.Infrastructure.Tests.Auth;

public class JwtBlacklistServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();

    private JwtBlacklistService CreateService()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_dbMock.Object);
        return new JwtBlacklistService(_redisMock.Object);
    }

    [Fact]
    public async Task RevokeUserBeforeAsync_SetsWatermarkWithTtl()
    {
        var service = CreateService();
        var userId = Guid.CreateVersion7();
        var revokedBefore = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        var ttl = TimeSpan.FromMinutes(70);

        RedisKey? capturedKey = null;
        RedisValue? capturedValue = null;
        Expiration? capturedExpiration = null;
        _dbMock.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((key, value, exp, _, _) =>
            {
                capturedKey = key;
                capturedValue = value;
                capturedExpiration = exp;
            })
            .ReturnsAsync(true);

        await service.RevokeUserBeforeAsync(userId, revokedBefore, ttl);

        capturedKey!.Value.ToString().Should().Be($"auth:user-revoked-before:{userId}");
        capturedValue!.Value.ToString().Should().Be(revokedBefore.ToString("O", CultureInfo.InvariantCulture));
        capturedExpiration.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeUserBeforeAsync_ZeroTtl_DoesNotWrite()
    {
        var service = CreateService();

        await service.RevokeUserBeforeAsync(Guid.CreateVersion7(), DateTimeOffset.UtcNow, TimeSpan.Zero);

        _dbMock.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task GetUserRevokedBeforeAsync_WhenMissing_ReturnsNull()
    {
        var service = CreateService();
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var result = await service.GetUserRevokedBeforeAsync(Guid.CreateVersion7());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserRevokedBeforeAsync_WhenSet_ReturnsParsedWatermark()
    {
        var service = CreateService();
        var revokedBefore = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(revokedBefore.ToString("O", CultureInfo.InvariantCulture)));

        var result = await service.GetUserRevokedBeforeAsync(Guid.CreateVersion7());

        result.Should().Be(revokedBefore);
    }
}