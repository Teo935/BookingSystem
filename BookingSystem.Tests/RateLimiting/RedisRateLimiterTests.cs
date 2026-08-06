using BookingSystem.Infrastructure.RateLimiting;
using Moq;
using StackExchange.Redis;

namespace BookingSystem.Tests.RateLimiting;

public class RedisRateLimiterTests
{
    private readonly Mock<IDatabase> _databaseMock;
    private readonly RedisRateLimiter _sut; // sut = System Under Test

    public RedisRateLimiterTests()
    {
        _databaseMock = new Mock<IDatabase>();
        var connectionMock = new Mock<IConnectionMultiplexer>();
        connectionMock
            .Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _sut = new RedisRateLimiter(connectionMock.Object);
    }

    [Fact]
    public async Task IsAllowedAsync_FirstRequestInWindow_SetsExpirationAndReturnsTrue()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.IsAllowedAsync("ratelimit:login:1.2.3.4", limit: 5, window: TimeSpan.FromSeconds(60));

        // Assert
        Assert.True(result);
        _databaseMock.Verify(
            d => d.KeyExpireAsync(It.IsAny<RedisKey>(), TimeSpan.FromSeconds(60), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task IsAllowedAsync_SubsequentRequestInWindow_DoesNotResetExpiration()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(3);

        // Act
        var result = await _sut.IsAllowedAsync("ratelimit:login:1.2.3.4", limit: 5, window: TimeSpan.FromSeconds(60));

        // Assert
        Assert.True(result);
        _databaseMock.Verify(
            d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()),
            Times.Never);
    }

    [Fact]
    public async Task IsAllowedAsync_CountWithinLimit_ReturnsTrue()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(5);

        // Act
        var result = await _sut.IsAllowedAsync("ratelimit:login:1.2.3.4", limit: 5, window: TimeSpan.FromSeconds(60));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAllowedAsync_CountExceedsLimit_ReturnsFalse()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(6);

        // Act
        var result = await _sut.IsAllowedAsync("ratelimit:login:1.2.3.4", limit: 5, window: TimeSpan.FromSeconds(60));

        // Assert
        Assert.False(result);
    }
}
