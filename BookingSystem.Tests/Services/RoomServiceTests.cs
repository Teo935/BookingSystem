using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using Moq;

namespace BookingSystem.Tests.Services;

public class RoomServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly RoomService _sut; // sut = System Under Test

    public RoomServiceTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _sut = new RoomService(_roomRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateRoomAsync_EmptyName_ReturnsError()
    {
        // Arrange
        var request = new CreateRoomRequest { Name = "", PricePerNight = 100 };

        // Act
        var (success, error, room) = await _sut.CreateRoomAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal("Name is required.", error);
        Assert.Null(room);
        _roomRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task CreateRoomAsync_WhitespaceName_ReturnsError()
    {
        // Arrange
        var request = new CreateRoomRequest { Name = "   ", PricePerNight = 100 };

        // Act
        var (success, error, room) = await _sut.CreateRoomAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal("Name is required.", error);
        Assert.Null(room);
        _roomRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task CreateRoomAsync_PriceZero_ReturnsError()
    {
        // Arrange
        var request = new CreateRoomRequest { Name = "Suite", PricePerNight = 0 };

        // Act
        var (success, error, room) = await _sut.CreateRoomAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal("PricePerNight must be greater than zero.", error);
        Assert.Null(room);
        _roomRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task CreateRoomAsync_NegativePrice_ReturnsError()
    {
        // Arrange
        var request = new CreateRoomRequest { Name = "Suite", PricePerNight = -50 };

        // Act
        var (success, error, room) = await _sut.CreateRoomAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal("PricePerNight must be greater than zero.", error);
        Assert.Null(room);
        _roomRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task CreateRoomAsync_ValidRequest_ReturnsSuccessAndCallsAddAsync()
    {
        // Arrange
        var request = new CreateRoomRequest { Name = "Suite", PricePerNight = 150 };
        _roomRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Room>()))
            .ReturnsAsync((Room room) => room);

        // Act
        var (success, error, room) = await _sut.CreateRoomAsync(request);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(room);
        Assert.Equal("Suite", room!.Name);
        Assert.Equal(150, room.PricePerNight);
        _roomRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Room>(x => x.Name == "Suite" && x.PricePerNight == 150)),
            Times.Once);
    }

    [Fact]
    public async Task GetAllRoomsAsync_ReturnsRepositoryResult()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Name = "Singola", PricePerNight = 80 },
            new() { Id = 2, Name = "Doppia", PricePerNight = 120 }
        };
        _roomRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        // Act
        var result = await _sut.GetAllRoomsAsync();

        // Assert
        Assert.Equal(rooms, result);
    }

    [Fact]
    public async Task GetRoomAsync_ExistingId_ReturnsRoom()
    {
        // Arrange
        var room = new Room { Id = 1, Name = "Suite", PricePerNight = 150 };
        _roomRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        // Act
        var result = await _sut.GetRoomAsync(1);

        // Assert
        Assert.Equal(room, result);
    }

    [Fact]
    public async Task GetRoomAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _roomRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);

        // Act
        var result = await _sut.GetRoomAsync(99);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRoomAsync_ExistingId_ReturnsUpdatedRoom()
    {
        // Arrange
        var request = new UpdateRoomRequest { Name = "Suite Deluxe", PricePerNight = 200 };
        var updatedRoom = new Room { Id = 1, Name = "Suite Deluxe", PricePerNight = 200 };
        _roomRepositoryMock
            .Setup(r => r.UpdateAsync(1, It.IsAny<Room>()))
            .ReturnsAsync(updatedRoom);

        // Act
        var result = await _sut.UpdateRoomAsync(1, request);

        // Assert
        Assert.Equal(updatedRoom, result);
    }

    [Fact]
    public async Task UpdateRoomAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var request = new UpdateRoomRequest { Name = "Suite Deluxe", PricePerNight = 200 };
        _roomRepositoryMock
            .Setup(r => r.UpdateAsync(99, It.IsAny<Room>()))
            .ReturnsAsync((Room?)null);

        // Act
        var result = await _sut.UpdateRoomAsync(99, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteRoomAsync_NonExistingRoom_ReturnsNotFound()
    {
        // Arrange
        _roomRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);

        // Act
        var result = await _sut.DeleteRoomAsync(99);

        // Assert
        Assert.Equal(RoomDeleteResult.NotFound, result);
        _roomRepositoryMock.Verify(r => r.HasBookingsAsync(It.IsAny<int>()), Times.Never);
        _roomRepositoryMock.Verify(r => r.RemoveAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRoomAsync_HasBookings_ReturnsConflict()
    {
        // Arrange
        var existingRoom = new Room { Id = 1, Name = "Suite", PricePerNight = 150 };
        _roomRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRoom);
        _roomRepositoryMock.Setup(r => r.HasBookingsAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteRoomAsync(1);

        // Assert
        Assert.Equal(RoomDeleteResult.Conflict, result);
        _roomRepositoryMock.Verify(r => r.RemoveAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRoomAsync_NoBookings_ReturnsSuccessAndCallsRemoveAsync()
    {
        // Arrange
        var existingRoom = new Room { Id = 1, Name = "Suite", PricePerNight = 150 };
        _roomRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRoom);
        _roomRepositoryMock.Setup(r => r.HasBookingsAsync(1)).ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteRoomAsync(1);

        // Assert
        Assert.Equal(RoomDeleteResult.Success, result);
        _roomRepositoryMock.Verify(r => r.RemoveAsync(existingRoom), Times.Once);
    }
}
