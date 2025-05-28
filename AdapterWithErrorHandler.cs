using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;
using System;
using WellbeingTeamsBot.Services;

namespace WellbeingTeamsBot
{
    public class AdapterWithErrorHandler : CloudAdapter
    {
        public AdapterWithErrorHandler(BotFrameworkAuthentication auth, ILogger<CloudAdapter> logger)
            : base(auth, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                var message = $"[AdapterWithErrorHandler] BOT ERROR: {exception.Message}\n{exception}";
                ManualLogger.Log(message);
                logger.LogError(exception, message);

                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https://www.botframework.com/schemas/error", "TurnError");
                await turnContext.SendActivityAsync("Oops. Something went wrong in the bot.");
            };
        }
    }
}
