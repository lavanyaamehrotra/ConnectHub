using NUnit.Framework;
using Moq;
using ConnectHub.NotificationService.Services;
using ConnectHub.NotificationService.Interfaces;
using ConnectHub.NotificationService.Models;
using ConnectHub.NotificationService.DTOs;
using ConnectHub.NotificationService.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NotificationServiceClass = ConnectHub.NotificationService.Services.NotificationService;

namespace ConnectHub.Tests
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private Mock<INotificationRepository> _repoMock;
        private Mock<INotificationPublisher> _publisherMock;
        private Mock<ILogger<NotificationServiceClass>> _loggerMock;
        private NotificationServiceClass _notificationService;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<INotificationRepository>();
            _publisherMock = new Mock<INotificationPublisher>();
            _loggerMock = new Mock<ILogger<NotificationServiceClass>>();
            _notificationService = new NotificationServiceClass(
                _repoMock.Object,
                _publisherMock.Object,
                _loggerMock.Object
            );
        }

        // TEST 1: Send Notification saves and publishes
        [Test]
        public async Task SendAsync_ValidNotification_SavesToRepoAndPublishes()
        {
            // Arrange
            var dto = new CreateNotificationDto
            {
                RecipientId = Guid.NewGuid(),
                SenderId = Guid.NewGuid(),
                Type = "MESSAGE",
                Title = "New Message",
                Message = "Hello"
            };

            _repoMock.Setup(r => r.SaveAsync(It.IsAny<Notification>()))
                     .ReturnsAsync((Notification n) => n);
            _repoMock.Setup(r => r.CountUnreadByRecipientAsync(dto.RecipientId)).ReturnsAsync(1);
            _publisherMock.Setup(p => p.PublishAsync(It.IsAny<NotificationEvent>())).Returns(Task.CompletedTask);

            // Act
            var result = await _notificationService.SendAsync(dto);

            // Assert
            Assert.That(result.Title, Is.EqualTo("New Message"));
            _repoMock.Verify(r => r.SaveAsync(It.IsAny<Notification>()), Times.Once);
            _publisherMock.Verify(p => p.PublishAsync(It.IsAny<NotificationEvent>()), Times.Once);
        }

        // TEST 2: Mark As Read updates status
        [Test]
        public async Task MarkAsReadAsync_ValidId_UpdatesStatus()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var notification = new Notification { NotificationId = notificationId, IsRead = false };
            _repoMock.Setup(r => r.FindByIdAsync(notificationId)).ReturnsAsync(notification);

            // Act
            await _notificationService.MarkAsReadAsync(notificationId);

            // Assert
            Assert.That(notification.IsRead, Is.True);
            _repoMock.Verify(r => r.UpdateAsync(notification), Times.Once);
        }

        // TEST 3: Get Unread Count returns correct count
        [Test]
        public async Task GetUnreadCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var recipientId = Guid.NewGuid();
            _repoMock.Setup(r => r.CountUnreadByRecipientAsync(recipientId)).ReturnsAsync(10);

            // Act
            var result = await _notificationService.GetUnreadCountAsync(recipientId);

            // Assert
            Assert.That(result, Is.EqualTo(10));
        }

        // TEST 4: Mark All Read calls repo
        [Test]
        public async Task MarkAllReadAsync_CallsRepo()
        {
            // Arrange
            var recipientId = Guid.NewGuid();

            // Act
            await _notificationService.MarkAllReadAsync(recipientId);

            // Assert
            _repoMock.Verify(r => r.MarkAllReadByRecipientAsync(recipientId), Times.Once);
        }

        // TEST 5: Get By Recipient returns list
        [Test]
        public async Task GetByRecipientAsync_ReturnsListOfDtos()
        {
            // Arrange
            var recipientId = Guid.NewGuid();
            var notifications = new List<Notification> { new Notification { Title = "Note 1" } };
            _repoMock.Setup(r => r.FindByRecipientAsync(recipientId, 1, 20)).ReturnsAsync(notifications);

            // Act
            var result = await _notificationService.GetByRecipientAsync(recipientId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Title, Is.EqualTo("Note 1"));
        }
    }
}
