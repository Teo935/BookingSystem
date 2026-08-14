using BookingSystem.Infrastructure.Identity;
using Moq;
using StackExchange.Redis;

namespace BookingSystem.Tests.Identity;

public class RedisRefreshTokenStoreTests
{
    private readonly Mock<IDatabase> _databaseMock;
    private readonly RedisRefreshTokenStore _sut; // sut = System Under Test

    public RedisRefreshTokenStoreTests()
    {
        _databaseMock = new Mock<IDatabase>();
        var connectionMock = new Mock<IConnectionMultiplexer>();
        connectionMock
            .Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _sut = new RedisRefreshTokenStore(connectionMock.Object);
    }

    [Fact]
    public async Task StoreAsync_CallsStringSetWithKeyPrefixValueAndTtl()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _sut.StoreAsync("token-abc", "user-1", TimeSpan.FromDays(7));

        // Assert
        _databaseMock.Verify(
            d => d.StringSetAsync(
                It.Is<RedisKey>(k => k == "refreshtoken:token-abc"),
                It.Is<RedisValue>(v => v == "user-1"),
                TimeSpan.FromDays(7),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserIdAsync_TokenExists_ReturnsUserId()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k == "refreshtoken:token-abc"), It.IsAny<CommandFlags>()))
            .ReturnsAsync("user-1");

        // Act
        var result = await _sut.GetUserIdAsync("token-abc");

        // Assert
        Assert.Equal("user-1", result);
    }

    [Fact]
    public async Task GetUserIdAsync_TokenDoesNotExist_ReturnsNull()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _sut.GetUserIdAsync("nonexistent-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_CallsKeyDeleteWithKeyPrefix()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _sut.RemoveAsync("token-abc");

        // Assert
        _databaseMock.Verify(
            d => d.KeyDeleteAsync(It.Is<RedisKey>(k => k == "refreshtoken:token-abc"), It.IsAny<CommandFlags>()),
            Times.Once);
    }
}
