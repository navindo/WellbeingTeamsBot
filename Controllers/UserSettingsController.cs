using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WellbeingTeamsBot.Storage;

namespace WellbeingTeamsBot.Controllers
{
    [ApiController]
    [Route("api/user/settings")]
    public class UserSettingsController : ControllerBase
    {
        private readonly ISqlStorageHelper _storageHelper;
        private readonly ILogger<UserSettingsController> _logger;

        public UserSettingsController(ISqlStorageHelper storageHelper, ILogger<UserSettingsController> logger)
        {
            _storageHelper = storageHelper;
            _logger = logger;
        }

        // GET /api/user/settings?objectId=xxxx
        [HttpGet]
        public async Task<IActionResult> GetSettings([FromQuery] string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId))
                return BadRequest("Missing required query parameter: objectId");

            try
            {
                var (enabled, snoozedUntil) = await _storageHelper.GetNotificationStatusAsync(objectId);

                return Ok(new
                {
                    objectId,
                    notificationsEnabled = enabled,
                    snoozedUntilUtc = snoozedUntil
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings for user: {objectId}", objectId);
                return StatusCode(500, "Failed to retrieve user settings.");
            }
        }

        // POST /api/user/settings
        [HttpPost]
        public async Task<IActionResult> UpdateSettings([FromBody] UserSettingsUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ObjectId))
                return BadRequest("Missing objectId");

            if (request.SnoozedUntilUtc.HasValue && request.SnoozedUntilUtc < DateTime.UtcNow)
                return BadRequest("SnoozedUntilUtc must be in the future.");

            try
            {
                await _storageHelper.UpdateNotificationStatusAsync(
                    request.ObjectId,
                    request.NotificationsEnabled,
                    request.SnoozedUntilUtc
                );

                return Ok("User settings updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings for user: {objectId}", request.ObjectId);
                return StatusCode(500, "Failed to update user settings.");
            }
        }
    }

    public class UserSettingsUpdateRequest
    {
        public string ObjectId { get; set; }
        public bool NotificationsEnabled { get; set; }
        public DateTime? SnoozedUntilUtc { get; set; }
    }
}
