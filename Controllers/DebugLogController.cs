using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace WellbeingTeamsBot.Controllers
{
    [ApiController]
    [Route("api/debuglog")]
    public class DebugLogController : ControllerBase
    {
        private readonly ILogger<DebugLogController> _logger;

        public DebugLogController(ILogger<DebugLogController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult TestLog()
        {
            var now = DateTime.UtcNow;

            // Log with ILogger
            _logger.LogInformation("📢 /api/debuglog called at {time}", now);

            // Log to manual file
            var logLine = $"[{now:u}] /api/debuglog hit\n";
            System.IO.File.AppendAllText("/home/LogFiles/manual-log.txt", logLine);

            return Ok("Both logs attempted.");
        }
    }
}
