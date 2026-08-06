using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Caching;
using Microsoft.Extensions.Options;
using Moq;

namespace BookingSystem.Tests.Services;

public class CachedRoomServiceTests
{
    private const string AllRoomsCacheKey = "rooms:all";

    private readonly Mock<IRoomService> _innerServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly CachedRoomService _sut; // sut = System Under Test

    public CachedRoomServiceTests()
    {
        _innerServiceMock = new Mock<IRoomService>();
        _cacheServiceMock = new Mock<ICacheService>();
        var cacheSettings = Options.Create(new CacheSettings { RoomsCacheDurationSeconds = 60 });
        _sut = new CachedRoomService(_innerServiceMock.Object, _cacheServiceMock.Object, cacheSettings);
    }

    [Fact]
    public async Task GetAllRoomsAsync_CacheHit_ReturnsCachedValueWithoutCallingInner()
    {
        // Arrange
        var cachedRooms = new List<Room> { new() { Id = 1, Name = "Suite", PricePerNight = 150 } };
        _cacheServiceMock.Setup(c => c.GetAsync<List<Room>>(AllRoomsCacheKey)).ReturnsAsync(cachedRooms);

        // Act
        var result = await _sut.GetAllRoomsAsync();

        // Assert
        Assert.Equal(cachedRooms, result);
        _innerServiceMock.Verify(s => s.GetAllRoomsAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAllRoomsAsync_CacheMiss_CallsInnerAndPopulatesCache()
    {
        // Arrange
        var rooms = new List<Room> { new() { Id = 1, Name = "Suite", PricePerNight = 150 } };
        _cacheServiceMock.Setup(c => c.GetAsync<List<Room>>(AllRoomsCacheKey)).ReturnsAsync((List<Room>?)null);
        _innerServiceMock.Setup(s => s.GetAllRoomsAsync()).ReturnsAsync(rooms);

        // Act
        var result = await _sut.GetAllRoomsAsync();

        // Assert
        Assert.Equal(rooms, result);
        _innerServiceMock.Verify(s => s.GetAllRoomsAsync(), Times.Once);
        _cacheServiceMock.Verify(c => c.SetAsync(AllRoomsCacheKey, rooms, TimeSpan.FromSeconds(60)), Times.Once);
    }

    [Fact]
    public async Task CreateRoomAsync_Success_InvalidatesCache()
    {
        // Arrange
        var request = new CreateRoomRequest { Name = "Suite", PricePerNight = 150 };
        var createdRoom = new Room { Id = 1, Name = "Suite", PricePerNight = 150 };
        _innerServiceMock.Setup(s => s.CreateRoomAsync(request)).ReturnsAsync((true, (string?)null, createdRoom));

        // Act
        var result = await _sut.CreateRoomAsync(request);

        // Assert
        Assert.True(result.Success);
        _cacheServiceMock.Verify(c => c.RemoveAsync(AllRoomsCacheKey), Times.Once);
    }

    [Fact]
    public async Task CreateRoomAsync_Failure_DoesNotInvalidateCache()
    {
        // Arrange
        var request = new CreateRoomRequest { Name = "", PricePerNight = 150 };
        _innerServiceMock.Setup(s => s.CreateRoomAsync(request)).ReturnsAsync((false, "Name is required.", (Room?)null));

        // Act
        var result = await _sut.CreateRoomAsync(request);

        // Assert
        Assert.False(result.Success);
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRoomAsync_ExistingRoom_InvalidatesCache()
    {
        // Arrange
        var request = new UpdateRoomRequest { Name = "Suite Deluxe", PricePerNight = 200 };
        var updatedRoom = new Room { Id = 1, Name = "Suite Deluxe", PricePerNight = 200 };
        _innerServiceMock.Setup(s => s.UpdateRoomAsync(1, request)).ReturnsAsync(updatedRoom);

        // Act
        var result = await _sut.UpdateRoomAsync(1, request);

        // Assert
        Assert.Equal(updatedRoom, result);
        _cacheServiceMock.Verify(c => c.RemoveAsync(AllRoomsCacheKey), Times.Once);
    }

    [Fact]
    public async Task UpdateRoomAsync_NonExistingRoom_DoesNotInvalidateCache()
    {
        // Arrange
        var request = new UpdateRoomRequest { Name = "Suite Deluxe", PricePerNight = 200 };
        _innerServiceMock.Setup(s => s.UpdateRoomAsync(99, request)).ReturnsAsync((Room?)null);

        // Act
        var result = await _sut.UpdateRoomAsync(99, request);

        // Assert
        Assert.Null(result);
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRoomAsync_Success_InvalidatesCache()
    {
        // Arrange
        _innerServiceMock.Setup(s => s.DeleteRoomAsync(1)).ReturnsAsync(RoomDeleteResult.Success);

        // Act
        var result = await _sut.DeleteRoomAsync(1);

        // Assert
        Assert.Equal(RoomDeleteResult.Success, result);
        _cacheServiceMock.Verify(c => c.RemoveAsync(AllRoomsCacheKey), Times.Once);
    }

    [Theory]
    [InlineData(RoomDeleteResult.NotFound)]
    [InlineData(RoomDeleteResult.Conflict)]
    public async Task DeleteRoomAsync_NotSuccess_DoesNotInvalidateCache(RoomDeleteResult deleteResult)
    {
        // Arrange
        _innerServiceMock.Setup(s => s.DeleteRoomAsync(1)).ReturnsAsync(deleteResult);

        // Act
        var result = await _sut.DeleteRoomAsync(1);

        // Assert
        Assert.Equal(deleteResult, result);
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetRoomAsync_DelegatesToInnerService()
    {
        // Arrange
        var room = new Room { Id = 1, Name = "Suite", PricePerNight = 150 };
        _innerServiceMock.Setup(s => s.GetRoomAsync(1)).ReturnsAsync(room);

        // Act
        var result = await _sut.GetRoomAsync(1);

        // Assert
        Assert.Equal(room, result);
        _cacheServiceMock.VerifyNoOtherCalls();
    }
}
