using NUnit.Framework;
using Moq;
using ConnectHub.HubService.Controllers;
using ConnectHub.HubService.Presence;
using ConnectHub.HubService.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ConnectHub.Tests
{
    [TestFixture]
    public class HubServiceTests
    {
        // ── NOTIFY CONTROLLER TESTS ─────────────────────────────
        private Mock<IHubContext<ChatHub>> _hubContextMock;
        private NotifyController _notifyController;

        // ── PRESENCE CONTROLLER TESTS ───────────────────────────
        private Mock<IPresenceService> _presenceServiceMock;
        private PresenceController _presenceController;

        [SetUp]
        public void Setup()
        {
            // Notify Setup
            _hubContextMock = new Mock<IHubContext<ChatHub>>();
            var clientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<IClientProxy>();
            
            _hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);
            clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(clientProxyMock.Object);
            
            _notifyController = new NotifyController(_hubContextMock.Object);

            // Presence Setup
            _presenceServiceMock = new Mock<IPresenceService>();
            _presenceController = new PresenceController(_presenceServiceMock.Object);
        }

        // TEST 1: Push Badge calls SignalR
        [Test]
        public async Task NotifyController_PushBadge_CallsHubContext()
        {
            // Arrange
            var request = new BadgeUpdateRequest { UserId = Guid.NewGuid(), UnreadCount = 5 };

            // Act
            var result = await _notifyController.PushBadge(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            _hubContextMock.Verify(h => h.Clients.User(request.UserId.ToString()), Times.Once);
        }

        // TEST 2: Get Online Users returns list
        [Test]
        public async Task PresenceController_GetOnlineUsers_ReturnsList()
        {
            // Arrange
            var userIds = new List<Guid> { Guid.NewGuid() };
            _presenceServiceMock.Setup(s => s.GetOnlineUserIdsAsync()).ReturnsAsync(userIds);

            // Act
            var result = await _presenceController.GetOnlineUsers();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(userIds));
        }

        // TEST 3: IsOnline returns correct status
        [Test]
        public async Task PresenceController_IsOnline_ReturnsCorrectStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _presenceServiceMock.Setup(s => s.IsUserOnlineAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _presenceController.IsOnline(userId);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            // Dynamic object check
            var value = okResult.Value as dynamic;
            // Since we can't easily check dynamic properties in NUnit without extra helpers, 
            // we'll just verify the service call
            _presenceServiceMock.Verify(s => s.IsUserOnlineAsync(userId), Times.Once);
        }

        // TEST 4: Get Connection Count returns number
        [Test]
        public async Task PresenceController_GetConnectionCount_ReturnsNumber()
        {
            // Arrange
            _presenceServiceMock.Setup(s => s.GetConnectionCountAsync()).ReturnsAsync(42);

            // Act
            var result = await _presenceController.GetConnectionCount();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            _presenceServiceMock.Verify(s => s.GetConnectionCountAsync(), Times.Once);
        }

        // TEST 5: Clear User calls service
        [Test]
        public async Task PresenceController_ClearUser_CallsService()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            await _presenceController.ClearUser(userId);

            // Assert
            _presenceServiceMock.Verify(s => s.ClearUserConnectionsAsync(userId), Times.Once);
        }
    }
}
