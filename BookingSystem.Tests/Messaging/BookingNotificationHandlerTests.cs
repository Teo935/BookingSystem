using BookingSystem.Application.Events;
using BookingSystem.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookingSystem.Tests.Messaging;

public class BookingNotificationHandlerTests
{
    private readonly Mock<ILogger<BookingNotificationHandler>> _loggerMock;
    private readonly BookingNotificationHandler _sut;

    public BookingNotificationHandlerTests()
    {
        _loggerMock = new Mock<ILogger<BookingNotificationHandler>>();
        _sut = new BookingNotificationHandler(_loggerMock.Object);
    }

    [Fact]
    public async Task HandleBookingCreatedAsync_ValidEvent_LogsGuestName()
    {
        // Arrange
        var evt = new BookingCreatedEvent(1, 1, "Suite", "user-1", "Mario Rossi",
            new DateTime(2026, 8, 1), new DateTime(2026, 8, 5), DateTime.UtcNow);

        // Act
        await _sut.HandleBookingCreatedAsync(evt);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Mario Rossi")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingCancelledAsync_ValidEvent_LogsGuestName()
    {
        // Arrange
        var evt = new BookingCancelledEvent(1, 1, "user-1", "Mario Rossi",
            new DateTime(2026, 8, 1), new DateTime(2026, 8, 5), DateTime.UtcNow);

        // Act
        await _sut.HandleBookingCancelledAsync(evt);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Mario Rossi")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
