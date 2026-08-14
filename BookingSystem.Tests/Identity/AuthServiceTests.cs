using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;
using BookingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace BookingSystem.Tests.Identity;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IRefreshTokenStore> _refreshTokenStoreMock;
    private readonly AuthService _sut; // sut = System Under Test

    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(7);

    public AuthServiceTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _refreshTokenStoreMock = new Mock<IRefreshTokenStore>();

        var jwtSettings = new JwtSettings
        {
            SecretKey = "test-secret-key-used-only-in-unit-tests-1234567890",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
        var tokenGenerator = new JwtTokenGenerator(jwtSettings);

        _sut = new AuthService(_userManagerMock.Object, tokenGenerator, _refreshTokenStoreMock.Object);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokensAndStoresRefreshToken()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-1", Email = "test@test.com", UserName = "test@test.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        // Act
        var (success, error, response) = await _sut.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "Password123!" });

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(response);
        Assert.False(string.IsNullOrEmpty(response!.Token));
        Assert.False(string.IsNullOrEmpty(response.RefreshToken));
        Assert.Equal("test@test.com", response.Email);
        Assert.Contains("User", response.Roles);
        _refreshTokenStoreMock.Verify(s => s.StoreAsync(response.RefreshToken!, "user-1", RefreshTokenTtl), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsErrorAndDoesNotStoreRefreshToken()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByEmailAsync("missing@test.com")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var (success, error, response) = await _sut.LoginAsync(new LoginRequest { Email = "missing@test.com", Password = "whatever" });

        // Assert
        Assert.False(success);
        Assert.Equal("Invalid email or password.", error);
        Assert.Null(response);
        _refreshTokenStoreMock.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsErrorAndDoesNotStoreRefreshToken()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-1", Email = "test@test.com", UserName = "test@test.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        // Act
        var (success, error, response) = await _sut.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "wrong" });

        // Assert
        Assert.False(success);
        Assert.Equal("Invalid email or password.", error);
        Assert.Null(response);
        _refreshTokenStoreMock.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsResponseWithoutRefreshToken()
    {
        // Arrange
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password123!"))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((u, _) => u.Id = "user-2");
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User")).ReturnsAsync(IdentityResult.Success);

        // Act
        var (success, error, response) = await _sut.RegisterAsync(new RegisterRequest { Email = "new@test.com", Password = "Password123!" });

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(response);
        Assert.Null(response!.RefreshToken);
        _refreshTokenStoreMock.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RotatesTokenAndReturnsNewTokens()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-1", Email = "test@test.com", UserName = "test@test.com" };
        _refreshTokenStoreMock.Setup(s => s.GetUserIdAsync("old-token")).ReturnsAsync("user-1");
        _userManagerMock.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        // Act
        var (success, error, response) = await _sut.RefreshAsync("old-token");

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(response);
        Assert.False(string.IsNullOrEmpty(response!.RefreshToken));
        Assert.NotEqual("old-token", response.RefreshToken);
        _refreshTokenStoreMock.Verify(s => s.RemoveAsync("old-token"), Times.Once);
        _refreshTokenStoreMock.Verify(s => s.StoreAsync(response.RefreshToken!, "user-1", RefreshTokenTtl), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_UnknownToken_ReturnsErrorWithoutTouchingStore()
    {
        // Arrange
        _refreshTokenStoreMock.Setup(s => s.GetUserIdAsync("bad-token")).ReturnsAsync((string?)null);

        // Act
        var (success, error, response) = await _sut.RefreshAsync("bad-token");

        // Assert
        Assert.False(success);
        Assert.Equal("Invalid or expired refresh token.", error);
        Assert.Null(response);
        _refreshTokenStoreMock.Verify(s => s.RemoveAsync(It.IsAny<string>()), Times.Never);
        _refreshTokenStoreMock.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_UserNoLongerExists_RemovesTokenAndReturnsError()
    {
        // Arrange: il refresh token in Redis punta a un utente che nel frattempo è stato eliminato.
        _refreshTokenStoreMock.Setup(s => s.GetUserIdAsync("orphan-token")).ReturnsAsync("deleted-user");
        _userManagerMock.Setup(m => m.FindByIdAsync("deleted-user")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var (success, error, response) = await _sut.RefreshAsync("orphan-token");

        // Assert
        Assert.False(success);
        Assert.Equal("Invalid or expired refresh token.", error);
        Assert.Null(response);
        _refreshTokenStoreMock.Verify(s => s.RemoveAsync("orphan-token"), Times.Once);
        _refreshTokenStoreMock.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_RemovesGivenRefreshTokenFromStore()
    {
        // Act
        await _sut.LogoutAsync("token-to-revoke");

        // Assert
        _refreshTokenStoreMock.Verify(s => s.RemoveAsync("token-to-revoke"), Times.Once);
    }
}
