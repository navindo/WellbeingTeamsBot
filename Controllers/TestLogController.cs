using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace WellbeingTeamsBot.Controllers
{
    [ApiController]
    [Route("api/testlog")]
    public class TestLogController : ControllerBase
    {
        private readonly ILogger<TestLogController> _logger;

        public TestLogController(ILogger<TestLogController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult LogTest()
        {
            _logger.LogInformation("🧪 TestLog endpoint was hit at {Time}", DateTime.UtcNow);
            return Ok("Logged test message.");
        }
    }
}
