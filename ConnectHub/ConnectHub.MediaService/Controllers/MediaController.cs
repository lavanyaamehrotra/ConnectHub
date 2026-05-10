using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ConnectHub.MediaService.Interfaces;

namespace ConnectHub.MediaService.Controllers
{
    // ============================================================
    // UC6 — MediaController
    // Route: api/media
    // Matches class diagram:
    //   POST   upload
    //   GET    byId / byUser / byRoom / byMessage / sasUrl
    //   DELETE delete
    //   GET    stats
    // ============================================================
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        // POST /api/media/upload
        // Upload a file to local storage (fallback) or Azure
        [Authorize] // Only logged in users can upload
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(
            IFormFile file,
            [FromQuery] int? messageId = null,
            [FromQuery] int? roomId    = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            var userId = GetUserId();

            try
            {
                var result = await _mediaService.UploadFileAsync(file, userId, messageId, roomId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/media/byId/{fileId}
        [HttpGet("byId/{fileId}")]
        public async Task<IActionResult> GetById(string fileId)
        {
            var file = await _mediaService.GetFileByIdAsync(fileId);
            if (file == null)
                return NotFound(new { message = "File not found." });

            return Ok(file);
        }

        // GET /api/media/byUser
        // Returns all files uploaded by the logged-in user
        [HttpGet("byUser")]
        public async Task<IActionResult> GetByUser()
        {
            var userId = GetUserId();
            var files  = await _mediaService.GetFilesByUserAsync(userId);
            return Ok(files);
        }

        // GET /api/media/byUser/{userId}
        // Returns files uploaded by a specific user (admin use)
        [HttpGet("byUser/{userId:guid}")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var files = await _mediaService.GetFilesByUserAsync(userId);
            return Ok(files);
        }

        // GET /api/media/byRoom/{roomId}
        [HttpGet("byRoom/{roomId:int}")]
        public async Task<IActionResult> GetByRoom(int roomId)
        {
            var files = await _mediaService.GetFilesByRoomAsync(roomId);
            return Ok(files);
        }

        // GET /api/media/byMessage/{messageId}
        [HttpGet("byMessage/{messageId:int}")]
        public async Task<IActionResult> GetByMessage(int messageId)
        {
            var files = await _mediaService.GetFilesByMessageAsync(messageId);
            return Ok(files);
        }

        // GET /api/media/sasUrl/{fileId}
        // Generate a secure time-limited SAS URL for downloading
        [HttpGet("sasUrl/{fileId}")]
        public async Task<IActionResult> GetSasUrl(string fileId)
        {
            try
            {
                var sasUrl = await _mediaService.GenerateSasUrlAsync(fileId);
                return Ok(new { fileId, sasUrl, expiresAt = DateTime.UtcNow.AddHours(1) });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "File not found." });
            }
        }

        // DELETE /api/media/{fileId}
        [Authorize] // Only logged in users can delete their files
        [HttpDelete("{fileId}")]
        public async Task<IActionResult> Delete(string fileId)
        {
            await _mediaService.DeleteFileAsync(fileId);
            return Ok(new { message = "File deleted successfully." });
        }

        // GET /api/media/download/{fileName}
        // Serves local files from the uploads folder (fallback)
        [AllowAnonymous]
        [HttpGet("download/{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var contentType = "application/octet-stream";
            if (fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg")) contentType = "image/jpeg";
            else if (fileName.EndsWith(".png")) contentType = "image/png";
            else if (fileName.EndsWith(".gif")) contentType = "image/gif";

            return PhysicalFile(filePath, contentType);
        }

        // GET /api/media/stats
        // Returns upload statistics (admin)
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _mediaService.GetFileStatsAsync();
            return Ok(stats);
        }

        // Helper: extract userId from JWT
        private Guid GetUserId()
        {
            var str = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? "";
            return Guid.TryParse(str, out var id) ? id : Guid.Empty;
        }
    }
}
