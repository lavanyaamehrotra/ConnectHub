using NUnit.Framework;
using Moq;
using ConnectHub.MessageService.Services;
using ConnectHub.MessageService.Interfaces;
using ConnectHub.MessageService.Models;
using ConnectHub.MessageService.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ConnectHub.Tests
{
    [TestFixture]
    public class MessageServiceTests
    {
        private Mock<IMessageRepository> _repoMock;
        private Mock<ILogger<MessageService.Services.MessageService>> _loggerMock;
        private MessageService.Services.MessageService _messageService;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IMessageRepository>();
            _loggerMock = new Mock<ILogger<MessageService.Services.MessageService>>();
            _messageService = new MessageService.Services.MessageService(_repoMock.Object, _loggerMock.Object);
        }

        // TEST 1: Send Message Saves to Repo
        [Test]
        public async Task SendMessageAsync_ValidRequest_SavesToRepo()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var request = new SendMessageRequest { ReceiverId = Guid.NewGuid(), Content = "Hello World" };

            // Act
            var result = await _messageService.SendMessageAsync(senderId, request);

            // Assert
            Assert.That(result.Content, Is.EqualTo("Hello World"));
            _repoMock.Verify(r => r.AddMessageAsync(It.IsAny<Message>()), Times.Once);
        }

        // TEST 2: Mark As Read Updates Status
        [Test]
        public async Task MarkAsReadAsync_ValidUser_UpdatesStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var message = new Message { MessageId = messageId, ReceiverId = userId, IsRead = false };
            _repoMock.Setup(r => r.FindByMessageIdAsync(messageId)).ReturnsAsync(message);

            // Act
            var result = await _messageService.MarkAsReadAsync(userId, messageId);

            // Assert
            Assert.That(result.IsRead, Is.True);
            _repoMock.Verify(r => r.UpdateMessageAsync(It.Is<Message>(m => m.IsRead == true)), Times.Once);
        }

        // TEST 3: Edit Message
        [Test]
        public async Task EditMessageAsync_ValidSender_UpdatesContent()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var message = new Message { MessageId = messageId, SenderId = senderId, Content = "Old Content" };
            _repoMock.Setup(r => r.FindByMessageIdAsync(messageId)).ReturnsAsync(message);

            var request = new EditMessageRequest { Content = "New Content" };

            // Act
            var result = await _messageService.EditMessageAsync(senderId, messageId, request);

            // Assert
            Assert.That(result.Content, Is.EqualTo("New Content"));
            Assert.That(result.IsEdited, Is.True);
            _repoMock.Verify(r => r.UpdateMessageAsync(message), Times.Once);
        }

        // TEST 4: Delete Message (Soft Delete)
        [Test]
        public async Task DeleteMessageAsync_ValidUser_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var message = new Message { MessageId = messageId, SenderId = userId, IsDeleted = false };
            _repoMock.Setup(r => r.FindByMessageIdAsync(messageId)).ReturnsAsync(message);
            _repoMock.Setup(r => r.DeleteByMessageIdAsync(messageId)).ReturnsAsync(true);

            // Act
            var result = await _messageService.DeleteMessageAsync(userId, messageId);

            // Assert
            Assert.That(result, Is.True);
            _repoMock.Verify(r => r.DeleteByMessageIdAsync(messageId), Times.Once);
        }

        // TEST 5: Get Unread Count
        [Test]
        public async Task GetUnreadCountAsync_HasUnread_ReturnsCount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.CountUnreadByReceiverIdAsync(userId)).ReturnsAsync(5);

            // Act
            var result = await _messageService.GetUnreadCountAsync(userId);

            // Assert
            Assert.That(result, Is.EqualTo(5));
        }
    }
}
