using NUnit.Framework;
using Moq;
using ConnectHub.ChatRoomService.Services;
using ConnectHub.ChatRoomService.Interfaces;
using ConnectHub.ChatRoomService.Models;
using ConnectHub.ChatRoomService.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ConnectHub.Tests
{
    [TestFixture]
    public class ChatRoomServiceTests
    {
        private Mock<IChatRoomRepository> _repoMock;
        private Mock<ILogger<ChatRoomService.Services.ChatRoomService>> _loggerMock;
        private ChatRoomService.Services.ChatRoomService _roomService;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IChatRoomRepository>();
            _loggerMock = new Mock<ILogger<ChatRoomService.Services.ChatRoomService>>();
            _roomService = new ChatRoomService.Services.ChatRoomService(_repoMock.Object, _loggerMock.Object);
        }

        // TEST 1: Create Room Saves Room and Admin
        [Test]
        public async Task CreateRoom_ValidRequest_SavesRoomAndAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateRoomRequest { RoomName = "Test Room", MaxMembers = 10, RoomType = "PUBLIC" };

            // Act
            var result = await _roomService.CreateRoom(userId, "admin_user", request);

            // Assert
            Assert.That(result.RoomName, Is.EqualTo("Test Room"));
            _repoMock.Verify(r => r.AddRoomAsync(It.IsAny<ChatRoom>()), Times.Once);
            _repoMock.Verify(r => r.AddMemberAsync(It.Is<RoomMember>(m => m.Role == "ADMIN")), Times.Once);
        }

        // TEST 2: Join Public Room Works
        [Test]
        public async Task JoinRoom_PublicRoom_SavesMember()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var room = new ChatRoom { RoomId = roomId, RoomType = "PUBLIC", MaxMembers = 10 };
            _repoMock.Setup(r => r.FindByRoomIdAsync(roomId)).ReturnsAsync(room);
            _repoMock.Setup(r => r.CountMembersByRoomIdAsync(roomId)).ReturnsAsync(5);
            _repoMock.Setup(r => r.IsUserInRoomAsync(userId, roomId)).ReturnsAsync(false);

            // Act
            var result = await _roomService.JoinRoom(userId, "new_member", roomId);

            // Assert
            Assert.That(result, Is.True);
            _repoMock.Verify(r => r.AddMemberAsync(It.Is<RoomMember>(m => m.UserId == userId)), Times.Once);
        }

        // TEST 3: Delete Room Deactivates It
        [Test]
        public async Task DeleteRoom_AsAdmin_DeactivatesRoom()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var room = new ChatRoom { RoomId = roomId, IsActive = true };
            var members = new List<RoomMember> { new RoomMember { UserId = adminId, Role = "ADMIN" } };
            
            _repoMock.Setup(r => r.FindByRoomIdAsync(roomId)).ReturnsAsync(room);
            _repoMock.Setup(r => r.FindMembersByRoomIdAsync(roomId)).ReturnsAsync(members);

            // Act
            var result = await _roomService.DeleteRoom(adminId, roomId);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(room.IsActive, Is.False);
            _repoMock.Verify(r => r.UpdateRoomAsync(room), Times.Once);
        }

        // TEST 4: Leave Room and Promote New Admin
        [Test]
        public async Task LeaveRoom_OnlyAdminLeaves_PromotesOthers()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var otherMemberId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var members = new List<RoomMember> { 
                new RoomMember { UserId = adminId, Role = "ADMIN", IsActive = true },
                new RoomMember { UserId = otherMemberId, Role = "MEMBER", IsActive = true }
            };
            _repoMock.Setup(r => r.FindMembersByRoomIdAsync(roomId)).ReturnsAsync(members);

            // Act
            var result = await _roomService.LeaveRoom(adminId, roomId);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(members[1].Role, Is.EqualTo("ADMIN"));
            _repoMock.Verify(r => r.UpdateMemberAsync(members[1]), Times.Once);
        }

        // TEST 5: Get Member Count
        [Test]
        public async Task GetMemberCount_ReturnsCorrectNumber()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            _repoMock.Setup(r => r.CountMembersByRoomIdAsync(roomId)).ReturnsAsync(15);

            // Act
            var result = await _roomService.GetMemberCount(roomId);

            // Assert
            Assert.That(result, Is.EqualTo(15));
        }
    }
}
