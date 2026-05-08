using NUnit.Framework;
using Moq;
using ConnectHub.AuthService.Services;
using ConnectHub.AuthService.Interfaces;
using ConnectHub.AuthService.Models;
using ConnectHub.AuthService.DTOs;
using ConnectHub.AuthService.Helpers;
using ConnectHub.AuthService.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ConnectHub.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _userRepoMock;
        private Mock<JwtHelper> _jwtHelperMock;
        private Mock<GoogleAuthHelper> _googleHelperMock;
        private Mock<ILogger<AuthService.Services.AuthService>> _loggerMock;
        private AuthService.Services.AuthService _authService;

        [SetUp]
        public void Setup()
        {
            _userRepoMock = new Mock<IUserRepository>();
            
            // Setup dummy settings to avoid NullReferenceException in constructors
            var jwtSettings = new JwtSettings { Secret = "this-is-a-very-secret-key-with-at-least-32-characters", ExpirationInMinutes = 60 };
            var googleSettings = new GoogleAuthSettings { ClientId = "google-client-id" };

            _jwtHelperMock = new Mock<JwtHelper>(Options.Create(jwtSettings));
            _googleHelperMock = new Mock<GoogleAuthHelper>(Options.Create(googleSettings), new Mock<ILogger<GoogleAuthHelper>>().Object);
            _loggerMock = new Mock<ILogger<AuthService.Services.AuthService>>();

            _authService = new AuthService.Services.AuthService(
                _userRepoMock.Object,
                _jwtHelperMock.Object,
                _googleHelperMock.Object,
                _loggerMock.Object
            );
        }

        // TEST 1: Successful Login
        [Test]
        public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var user = new User 
            { 
                Username = "testuser", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                IsActive = true 
            };
            _userRepoMock.Setup(r => r.GetByUsernameOrEmailAsync("testuser")).ReturnsAsync(user);
            _jwtHelperMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("mock-token");

            var request = new LoginRequest { UsernameOrEmail = "testuser", Password = "password123" };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token, Is.EqualTo("mock-token"));
        }

        // TEST 2: Failed Login (Wrong Password)
        [Test]
        public async Task LoginAsync_InvalidPassword_ReturnsFailure()
        {
            // Arrange
            var user = new User 
            { 
                Username = "testuser", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-pass") 
            };
            _userRepoMock.Setup(r => r.GetByUsernameOrEmailAsync("testuser")).ReturnsAsync(user);

            var request = new LoginRequest { UsernameOrEmail = "testuser", Password = "wrong-password" };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Invalid username/email or password"));
        }

        // TEST 3: Registration with existing username fails
        [Test]
        public async Task RegisterAsync_ExistingUsername_ReturnsFailure()
        {
            // Arrange
            _userRepoMock.Setup(r => r.UsernameExistsAsync("existing")).ReturnsAsync(true);
            var request = new RegisterRequest { Username = "existing", Email = "new@test.com", Password = "pass" };

            // Act
            var result = await _authService.RegisterAsync(request);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Username already taken. Please choose another."));
        }

        // TEST 4: Profile update works
        [Test]
        public async Task UpdateProfileAsync_ValidUser_UpdatesFields()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, DisplayName = "Old Name" };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var request = new UpdateProfileRequest { DisplayName = "New Name", Bio = "I love testing" };

            // Act
            var result = await _authService.UpdateProfileAsync(userId, request);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.User?.DisplayName, Is.EqualTo("New Name"));
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        // TEST 5: Logout updates online status
        [Test]
        public async Task LogoutAsync_ValidUser_SetsOffline()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, IsOnline = true };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _authService.LogoutAsync(userId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(user.IsOnline, Is.False);
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }
    }
}
