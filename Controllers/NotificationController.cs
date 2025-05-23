using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using WellbeingTeamsBot.Services;
using WellbeingTeamsBot.Storage;

namespace WellbeingTeamsBot.Controllers
{
    [ApiController]
    [Route("api/notify")]
    public class NotificationController : ControllerBase
    {
        private readonly IAlertService _alertService;
        private readonly ISqlStorageHelper _storageHelper;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            IAlertService alertService,
            ISqlStorageHelper storageHelper,
            ILogger<NotificationController> logger)
        {
            _alertService = alertService;
            _storageHelper = storageHelper;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendAdaptiveCard()
        {
            var logFile = "/home/LogFiles/manual-log.txt";

            try
            {
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                _logger.LogInformation("📥 RAW REQUEST BODY: {raw}", rawBody);
                System.IO.File.AppendAllText(logFile, $"[{DateTime.UtcNow:u}] /api/notify called\n");

                var request = JsonConvert.DeserializeObject<NotifyRequest>(rawBody);

                if (string.IsNullOrWhiteSpace(request.ObjectId) || request.MessageCardJson == null)
                {
                    System.IO.File.AppendAllText(logFile, $"[{DateTime.UtcNow:u}] ❌ Missing ObjectId or MessageCardJson\n");
                    return BadRequest("Both objectId and messageCardJson are required.");
                }

                System.IO.File.AppendAllText(logFile, $"[{DateTime.UtcNow:u}] Sending card to {request.ObjectId}\n");
                await _alertService.SendCardAsync(request.ObjectId, request.MessageCardJson);
                _logger.LogInformation("Card sent via AlertService to {ObjectId}", request.ObjectId);
                System.IO.File.AppendAllText(logFile, $"[{DateTime.UtcNow:u}] ✅ Card sent to {request.ObjectId}\n");

                await _storageHelper.UpdateLastAlertSentAsync(request.ObjectId);
                _logger.LogInformation("LastAlertSent timestamp updated for {ObjectId}", request.ObjectId);
                System.IO.File.AppendAllText(logFile, $"[{DateTime.UtcNow:u}] 📅 LastAlertSent updated for {request.ObjectId}\n");

                return Ok("Card sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send card");
                System.IO.File.AppendAllText(logFile, $"[{DateTime.UtcNow:u}] 💥 ERROR: {ex}\n");
                return StatusCode(500, "Internal error while sending card.");
            }
        }
    }

    public class NotifyRequest
    {
        public string ObjectId { get; set; }
        public JObject MessageCardJson { get; set; }
    }
}
