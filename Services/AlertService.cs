using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using WellbeingTeamsBot.Storage;
using WellbeingTeamsBot.Services;

namespace WellbeingTeamsBot.Services
{
    public class AlertService : IAlertService
    {
        private readonly CloudAdapter _adapter;
        private readonly string _appId;
        private readonly ISqlStorageHelper _storageHelper;
        private readonly ILogger<AlertService> _logger;

        public AlertService(
            CloudAdapter adapter,
            IConfiguration config,
            ISqlStorageHelper storageHelper,
            ILogger<AlertService> logger)
        {
            _adapter = adapter;
            _appId = config["MicrosoftAppId"];
            _storageHelper = storageHelper;
            _logger = logger;
        }

        public async Task SendCardAsync(string objectId, JObject adaptiveCardJson)
        {
            try
            {
                ManualLogger.Log($"SendCardAsync invoked for objectId={objectId}");

                var reference = await _storageHelper.GetReferenceAsync(objectId);
                if (reference == null)
                {
                    var msg = $"No conversation reference found for user {objectId}";
                    _logger.LogWarning(msg);
                    ManualLogger.Log(msg);
                    return;
                }

                var attachment = new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = adaptiveCardJson
                };

                await _adapter.ContinueConversationAsync(
                    _appId,
                    reference,
                    async (context, token) =>
                    {
                        await context.SendActivityAsync(MessageFactory.Attachment(attachment));
                    },
                    default
                );

                await _storageHelper.UpdateLastAlertSentAsync(objectId);
                _logger.LogInformation("Card sent and alert time updated for user {ObjectId}", objectId);
                ManualLogger.Log($"Card sent and alert time updated for user {objectId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send card for objectId={ObjectId}", objectId);
                ManualLogger.Log($"[AlertService] ERROR for objectId={objectId}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
    }
}
