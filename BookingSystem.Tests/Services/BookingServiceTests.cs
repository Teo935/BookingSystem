using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Events;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using Moq;

namespace BookingSystem.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly BookingService _sut;

    public BookingServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _sut = new BookingService(_bookingRepositoryMock.Object, _roomRepositoryMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task CreateBookingAsync_CheckInEqualsCheckOut_ReturnsError()
    {
        // Arrange
        var sameDate = new DateTime(2026, 8, 1);
        var request = new CreateBookingRequest
        {
            RoomId = 1,
            GuestName = "Mario Rossi",
            CheckIn = sameDate,
            CheckOut = sameDate
        };

        // Act
        var (success, error, booking) = await _sut.CreateBookingAsync(request, "user-1");

        // Assert
        Assert.False(success);
        Assert.Equal("CheckIn date must be before CheckOut date.", error);
        Assert.Null(booking);
        _bookingRepositoryMock.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<BookingCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_CheckInAfterCheckOut_ReturnsError()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            RoomId = 1,
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 10),
            CheckOut = new DateTime(2026, 8, 5)
        };

        // Act
        var (success, error, booking) = await _sut.CreateBookingAsync(request, "user-1");

        // Assert
        Assert.False(success);
        Assert.Equal("CheckIn date must be before CheckOut date.", error);
        Assert.Null(booking);
        _bookingRepositoryMock.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<BookingCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_RoomDoesNotExist_ReturnsError()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            RoomId = 1,
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 1),
            CheckOut = new DateTime(2026, 8, 5)
        };
        _roomRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Room?)null);

        // Act
        var (success, error, booking) = await _sut.CreateBookingAsync(request, "user-1");

        // Assert
        Assert.False(success);
        Assert.Equal("Room does not exist.", error);
        Assert.Null(booking);
        _bookingRepositoryMock.Verify(b => b.HasOverlapAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        _bookingRepositoryMock.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<BookingCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_OverlappingDates_ReturnsError()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            RoomId = 1,
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 1),
            CheckOut = new DateTime(2026, 8, 5)
        };
        _roomRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Room { Id = 1, Name = "Suite", PricePerNight = 150 });
        _bookingRepositoryMock
            .Setup(b => b.HasOverlapAsync(1, request.CheckIn, request.CheckOut))
            .ReturnsAsync(true);

        // Act
        var (success, error, booking) = await _sut.CreateBookingAsync(request, "user-1");

        // Assert
        Assert.False(success);
        Assert.Equal("Room is already booked for the selected dates.", error);
        Assert.Null(booking);
        _bookingRepositoryMock.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<BookingCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_ValidRequest_ReturnsSuccessAndCallsAddAsync()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            RoomId = 1,
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 1),
            CheckOut = new DateTime(2026, 8, 5)
        };
        _roomRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Room { Id = 1, Name = "Suite", PricePerNight = 150 });
        _bookingRepositoryMock
            .Setup(b => b.HasOverlapAsync(1, request.CheckIn, request.CheckOut))
            .ReturnsAsync(false);
        _bookingRepositoryMock
            .Setup(b => b.AddAsync(It.IsAny<Booking>()))
            .ReturnsAsync((Booking booking) => booking);

        var before = DateTime.UtcNow;

        // Act
        var (success, error, booking) = await _sut.CreateBookingAsync(request, "user-1");

        var after = DateTime.UtcNow;

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(booking);
        Assert.Equal("Mario Rossi", booking!.GuestName);
        Assert.Equal(1, booking.RoomId);
        Assert.Equal("user-1", booking.UserId);
        Assert.InRange(booking.CreatedAt, before, after);
        _bookingRepositoryMock.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Once);
        _eventPublisherMock.Verify(p => p.PublishAsync(
            It.Is<BookingCreatedEvent>(e => e.RoomId == 1 && e.GuestName == "Mario Rossi" && e.RoomName == "Suite"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_NoOverlap_ReturnsTrue()
    {
        // Arrange
        var checkIn = new DateTime(2026, 8, 1);
        var checkOut = new DateTime(2026, 8, 5);
        _bookingRepositoryMock.Setup(b => b.HasOverlapAsync(1, checkIn, checkOut)).ReturnsAsync(false);

        // Act
        var result = await _sut.IsRoomAvailableAsync(1, checkIn, checkOut);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_HasOverlap_ReturnsFalse()
    {
        // Arrange
        var checkIn = new DateTime(2026, 8, 1);
        var checkOut = new DateTime(2026, 8, 5);
        _bookingRepositoryMock.Setup(b => b.HasOverlapAsync(1, checkIn, checkOut)).ReturnsAsync(true);

        // Act
        var result = await _sut.IsRoomAvailableAsync(1, checkIn, checkOut);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_RoomDoesNotExist_StillOnlyChecksOverlap()
    {
        // Arrange: il metodo non verifica l'esistenza della room, si basa solo
        // sull'overlap delle date. Questo test fissa il comportamento attuale,
        // non lo "corregge".
        var checkIn = new DateTime(2026, 8, 1);
        var checkOut = new DateTime(2026, 8, 5);
        _roomRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Room?)null);
        _bookingRepositoryMock.Setup(b => b.HasOverlapAsync(99, checkIn, checkOut)).ReturnsAsync(false);

        // Act
        var result = await _sut.IsRoomAvailableAsync(99, checkIn, checkOut);

        // Assert
        Assert.True(result);
        _roomRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetBookingAsync_OwnerRequestsOwnBooking_ReturnsSuccessAndBooking()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            RoomId = 1,
            Room = new Room { Id = 1, Name = "Suite", PricePerNight = 150 },
            UserId = "user-1",
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 1),
            CheckOut = new DateTime(2026, 8, 5)
        };
        _bookingRepositoryMock.Setup(b => b.GetByIdWithRoomAsync(1)).ReturnsAsync(booking);

        // Act
        var (result, returnedBooking) = await _sut.GetBookingAsync(1, "user-1", isAdmin: false);

        // Assert
        Assert.Equal(BookingAccessResult.Success, result);
        Assert.Equal(booking, returnedBooking);
        _bookingRepositoryMock.Verify(b => b.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetBookingAsync_AdminRequestsAnyBooking_ReturnsSuccessAndBooking()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            RoomId = 1,
            Room = new Room { Id = 1, Name = "Suite", PricePerNight = 150 },
            UserId = "user-1",
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 1),
            CheckOut = new DateTime(2026, 8, 5)
        };
        _bookingRepositoryMock.Setup(b => b.GetByIdWithRoomAsync(1)).ReturnsAsync(booking);

        // Act
        var (result, returnedBooking) = await _sut.GetBookingAsync(1, "admin-1", isAdmin: true);

        // Assert
        Assert.Equal(BookingAccessResult.Success, result);
        Assert.Equal(booking, returnedBooking);
    }

    [Fact]
    public async Task GetBookingAsync_NonOwnerNonAdminRequestsBooking_ReturnsForbidden()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            RoomId = 1,
            Room = new Room { Id = 1, Name = "Suite", PricePerNight = 150 },
            UserId = "user-1",
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 1),
            CheckOut = new DateTime(2026, 8, 5)
        };
        _bookingRepositoryMock.Setup(b => b.GetByIdWithRoomAsync(1)).ReturnsAsync(booking);

        // Act
        var (result, returnedBooking) = await _sut.GetBookingAsync(1, "user-2", isAdmin: false);

        // Assert
        Assert.Equal(BookingAccessResult.Forbidden, result);
        Assert.Null(returnedBooking);
    }

    [Fact]
    public async Task GetBookingAsync_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _bookingRepositoryMock.Setup(b => b.GetByIdWithRoomAsync(99)).ReturnsAsync((Booking?)null);

        // Act
        var (result, returnedBooking) = await _sut.GetBookingAsync(99, "user-1", isAdmin: false);

        // Assert
        Assert.Equal(BookingAccessResult.NotFound, result);
        Assert.Null(returnedBooking);
    }

    [Fact]
    public async Task CancelBookingAsync_NonExistingBooking_ReturnsNotFound()
    {
        // Arrange
        _bookingRepositoryMock.Setup(b => b.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        // Act
        var result = await _sut.CancelBookingAsync(99, "user-1", isAdmin: false);

        // Assert
        Assert.Equal(BookingAccessResult.NotFound, result);
        _bookingRepositoryMock.Verify(b => b.RemoveAsync(It.IsAny<Booking>()), Times.Never);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<BookingCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_OwnerCancelsOwnBooking_ReturnsSuccessAndCallsRemoveAsync()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            RoomId = 1,
            UserId = "user-1",
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 1),
            CheckOut = new DateTime(2026, 8, 5)
        };
        _bookingRepositoryMock.Setup(b => b.GetByIdAsync(1)).ReturnsAsync(booking);

        // Act
        var result = await _sut.CancelBookingAsync(1, "user-1", isAdmin: false);

        // Assert
        Assert.Equal(BookingAccessResult.Success, result);
        _bookingRepositoryMock.Verify(b => b.RemoveAsync(booking), Times.Once);
        _eventPublisherMock.Verify(p => p.PublishAsync(
            It.Is<BookingCancelledEvent>(e => e.BookingId == 1 && e.GuestName == "Mario Rossi"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_AdminCancelsAnyBooking_ReturnsSuccessAndCallsRemoveAsync()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            RoomId = 1,
            UserId = "user-1",
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 1),
            CheckOut = new DateTime(2026, 8, 5)
        };
        _bookingRepositoryMock.Setup(b => b.GetByIdAsync(1)).ReturnsAsync(booking);

        // Act
        var result = await _sut.CancelBookingAsync(1, "admin-1", isAdmin: true);

        // Assert
        Assert.Equal(BookingAccessResult.Success, result);
        _bookingRepositoryMock.Verify(b => b.RemoveAsync(booking), Times.Once);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<BookingCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_NonOwnerNonAdminCancelsBooking_ReturnsForbiddenAndDoesNotCallRemoveAsync()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            RoomId = 1,
            UserId = "user-1",
            GuestName = "Mario Rossi",
            CheckIn = new DateTime(2026, 8, 1),
            CheckOut = new DateTime(2026, 8, 5)
        };
        _bookingRepositoryMock.Setup(b => b.GetByIdAsync(1)).ReturnsAsync(booking);

        // Act
        var result = await _sut.CancelBookingAsync(1, "user-2", isAdmin: false);

        // Assert
        Assert.Equal(BookingAccessResult.Forbidden, result);
        _bookingRepositoryMock.Verify(b => b.RemoveAsync(It.IsAny<Booking>()), Times.Never);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<BookingCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
