using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ConnectHub.NotificationService.Controllers
{
    [ApiController]
    [Route("api/notifications/status")]
    public class StatusController : ControllerBase
    {
        private readonly IConfiguration _config;

        public StatusController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            var smtpHost = _config["Email:SmtpHost"];
            var smtpUser = _config["Email:SmtpUser"];
            var hasPass  = !string.IsNullOrEmpty(_config["Email:SmtpPass"]);
            var rabbit   = _config["RabbitMQ:Host"] ?? _config["RabbitMQ:Url"] ?? "Not Set";

            return Ok(new
            {
                Service = "NotificationService",
                Status = "Running",
                Configuration = new
                {
                    SmtpHost = smtpHost ?? "MISSING",
                    SmtpUser = smtpUser ?? "MISSING",
                    SmtpPasswordSet = hasPass,
                    RabbitMqSource = rabbit
                },
                Instructions = "To enable emails, set Email__SmtpUser and Email__SmtpPass in Render env vars."
            });
        }
    }
}
