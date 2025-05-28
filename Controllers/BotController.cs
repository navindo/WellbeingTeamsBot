using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using WellbeingTeamsBot.Services;
using System;
using System.Threading.Tasks;

namespace WellbeingTeamsBot.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;

        public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
        {
            _adapter = adapter;
            _bot = bot;
        }

        [HttpPost]
        public async Task PostAsync()
        {
            ManualLogger.Log(">>> POST /api/messages received");

            try
            {
                await _adapter.ProcessAsync(Request, Response, _bot);
            }
            catch (Exception ex)
            {
                ManualLogger.Log($"[BotController] ERROR in /api/messages: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
    }
}
