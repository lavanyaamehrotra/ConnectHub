using NUnit.Framework;
using Moq;
using ConnectHub.MediaService.Services;
using ConnectHub.MediaService.Interfaces;
using ConnectHub.MediaService.Models;
using ConnectHub.MediaService.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MediaServiceClass = ConnectHub.MediaService.Services.MediaService;

namespace ConnectHub.Tests
{
    [TestFixture]
    public class MediaServiceTests
    {
        private Mock<IMediaRepository> _repoMock;
        private Mock<BlobServiceClient> _blobServiceClientMock;
        private Mock<IConfiguration> _configMock;
        private Mock<ILogger<MediaServiceClass>> _loggerMock;
        private MediaServiceClass _mediaService;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IMediaRepository>();
            _blobServiceClientMock = new Mock<BlobServiceClient>();
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<MediaServiceClass>>();

            // Default config values
            _configMock.Setup(c => c["Azure:MaxFileSizeMb"]).Returns("50");
            _configMock.Setup(c => c["Azure:AllowedContentTypes"]).Returns("image/,video/,application/pdf,text/");

            _mediaService = new MediaServiceClass(
                _repoMock.Object,
                _blobServiceClientMock.Object,
                _configMock.Object,
                _loggerMock.Object
            );
        }

        // TEST 1: File too large throws exception
        [Test]
        public void UploadFileAsync_FileTooLarge_ThrowsException()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(60 * 1024 * 1024); // 60MB
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _mediaService.UploadFileAsync(fileMock.Object, Guid.NewGuid(), null, null));
            Assert.That(ex.Message, Does.Contain("exceeds maximum size"));
        }

        // TEST 2: Invalid type throws exception
        [Test]
        public void UploadFileAsync_InvalidType_ThrowsException()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1 * 1024 * 1024);
            fileMock.Setup(f => f.ContentType).Returns("application/x-msdownload"); // .exe
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _mediaService.UploadFileAsync(fileMock.Object, Guid.NewGuid(), null, null));
            Assert.That(ex.Message, Does.Contain("not allowed"));
        }

        // TEST 3: Get File By Id returns DTO
        [Test]
        public async Task GetFileByIdAsync_ExistingFile_ReturnsDto()
        {
            // Arrange
            var fileId = "test-id";
            var mediaFile = new MediaFile { FileId = fileId, FileName = "test.png" };
            _repoMock.Setup(r => r.FindByFieldId(fileId)).ReturnsAsync(mediaFile);

            // Act
            var result = await _mediaService.GetFileByIdAsync(fileId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.FileName, Is.EqualTo("test.png"));
        }

        // TEST 4: Delete File calls repo
        [Test]
        public async Task DeleteFileAsync_ValidId_CallsRepo()
        {
            // Arrange
            var fileId = "delete-id";
            var mediaFile = new MediaFile { FileId = fileId, BlobUrl = "http://blob.com/container/blob" };
            _repoMock.Setup(r => r.FindByFieldId(fileId)).ReturnsAsync(mediaFile);
            
            // Mock Blob delete
            var containerMock = new Mock<BlobContainerClient>();
            var blobMock = new Mock<BlobClient>();
            _blobServiceClientMock.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(containerMock.Object);
            containerMock.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blobMock.Object);

            // Act
            await _mediaService.DeleteFileAsync(fileId);

            // Assert
            _repoMock.Verify(r => r.DeleteByFileId(fileId), Times.Once);
        }

        // TEST 5: Get File Stats returns aggregates
        [Test]
        public async Task GetFileStatsAsync_ReturnsCorrectAggregates()
        {
            // Arrange
            var files = new List<MediaFile> 
            { 
                new MediaFile { ContentType = "image/png", FileSizeKb = 100 },
                new MediaFile { ContentType = "video/mp4", FileSizeKb = 500 }
            };
            _repoMock.Setup(r => r.FindAllAsync(1, int.MaxValue)).ReturnsAsync(files);

            // Act
            var result = await _mediaService.GetFileStatsAsync();

            // Assert
            Assert.That(result.TotalFiles, Is.EqualTo(2));
            Assert.That(result.ImageCount, Is.EqualTo(1));
            Assert.That(result.VideoCount, Is.EqualTo(1));
            Assert.That(result.TotalSizeKb, Is.EqualTo(600));
        }
    }
}
