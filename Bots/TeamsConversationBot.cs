using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using WellbeingTeamsBot.Storage;
using WellbeingTeamsBot.Services;

namespace WellbeingTeamsBot.Bots
{
    public class TeamsConversationBot : TeamsActivityHandler
    {
        private readonly ISqlStorageHelper _storageHelper;
        private readonly ILogger<TeamsConversationBot> _logger;

        public TeamsConversationBot(ISqlStorageHelper storageHelper, ILogger<TeamsConversationBot> logger)
        {
            _storageHelper = storageHelper;
            _logger = logger;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var reference = turnContext.Activity.GetConversationReference();
            string userId;

            try
            {
                var member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);
                userId = member.AadObjectId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user profile from TeamsInfo.");
                userId = turnContext.Activity.From?.Id ?? "unknown";
                ManualLogger.Log($"[GetMemberAsync failed] FromId={turnContext.Activity.From?.Id}, Error={ex.Message}\n{ex.StackTrace}");
            }

            try
            {
                await _storageHelper.StoreOrUpdateReferenceAsync(userId, reference);
                _logger.LogInformation("Stored conversation reference for user: {userId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store conversation reference for user: {userId}", userId);
                ManualLogger.Log($"[StoreOrUpdateReferenceAsync failed] userId={userId}, Error={ex.Message}\n{ex.StackTrace}");
            }

            var text = turnContext.Activity.Text?.Trim().ToLower();

            if (string.IsNullOrEmpty(text) && turnContext.Activity.Value is not null)
            {
                var payload = turnContext.Activity.Value as dynamic;
                if (payload?.command != null)
                {
                    text = payload.command.ToString().ToLower();
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                await turnContext.SendActivityAsync("Please enter a command like 'stop notifications', 'start notifications', 'snooze for 24 hours', 'snooze for 1 hour', or 'resume now'.");
                return;
            }

            switch (text)
            {
                case "stop notifications":
                    await _storageHelper.UpdateNotificationStatusAsync(userId, enabled: false, snoozedUntilUtc: null);
                    await turnContext.SendActivityAsync("Notifications have been stopped. You won't receive further alerts until resumed.");
                    break;

                case "start notifications":
                    await _storageHelper.UpdateNotificationStatusAsync(userId, enabled: true, snoozedUntilUtc: null);
                    await turnContext.SendActivityAsync("Notifications have been resumed.");
                    break;

                case "snooze for 24 hours":
                    await _storageHelper.UpdateNotificationStatusAsync(userId, enabled: true, snoozedUntilUtc: DateTime.UtcNow.AddHours(24));
                    await turnContext.SendActivityAsync("Notifications snoozed for 24 hours.");
                    break;

                case "snooze for 1 hour":
                    await _storageHelper.UpdateNotificationStatusAsync(userId, enabled: true, snoozedUntilUtc: DateTime.UtcNow.AddHours(1));
                    await turnContext.SendActivityAsync("Notifications snoozed for 1 hour.");
                    break;

                case "resume now":
                    await _storageHelper.UpdateNotificationStatusAsync(userId, enabled: true, snoozedUntilUtc: null);
                    await turnContext.SendActivityAsync("Notifications resumed immediately.");
                    break;

                default:
                    await turnContext.SendActivityAsync("Unknown command. Please use 'stop notifications', 'start notifications', 'snooze for 24 hours', 'snooze for 1 hour', or 'resume now'.");
                    break;
            }
        }

        protected override async Task OnInstallationUpdateActivityAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bot received installation update activity");

            if (turnContext.Activity.Conversation.ConversationType == "personal")
            {
                try
                {
                    _logger.LogInformation("Detected personal conversation");
                    ManualLogger.Log("[InstallUpdate] Personal conversation detected.");

                    var reference = turnContext.Activity.GetConversationReference();
                    _logger.LogInformation("Got conversation reference");

                    var objectId = turnContext.Activity.From?.AadObjectId ?? turnContext.Activity.From?.Id;
                    var userName = turnContext.Activity.From?.Name;
                    var userId = turnContext.Activity.From?.Id;

                    var logEntry = $"[InstallUpdate] App installed by user: {userName}, TeamsId: {userId}, ObjectId: {objectId}";
                    _logger.LogInformation(logEntry);
                    ManualLogger.Log(logEntry);

                    await _storageHelper.StoreOrUpdateReferenceAsync(objectId, reference);
                    _logger.LogInformation("Stored reference");

                    var adaptiveCardJson = @"{
                        ""type"": ""AdaptiveCard"",
                        ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
                        ""version"": ""1.4"",
                        ""body"": [
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""Hi! I'm your Well-being Assistant"",
                                ""weight"": ""Bolder"",
                                ""size"": ""Medium""
                            },
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""I'll help you take timely breaks during your workday. Use the buttons below or type a command to control notifications."",
                                ""wrap"": true
                            }
                        ],
                        ""actions"": [
                            {
                                ""type"": ""Action.Submit"",
                                ""title"": ""Stop Notifications"",
                                ""data"": { ""command"": ""stop notifications"" }
                            },
                            {
                                ""type"": ""Action.Submit"",
                                ""title"": ""Start Notifications"",
                                ""data"": { ""command"": ""start notifications"" }
                            },
                            {
                                ""type"": ""Action.Submit"",
                                ""title"": ""Snooze 24 Hours"",
                                ""data"": { ""command"": ""snooze for 24 hours"" }
                            },
                            {
                                ""type"": ""Action.Submit"",
                                ""title"": ""Resume Now"",
                                ""data"": { ""command"": ""resume now"" }
                            }
                        ]
                    }";

                    _logger.LogInformation("Parsed adaptive card JSON");

                    var attachment = new Attachment
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCardJson)
                    };

                    try
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
                        _logger.LogInformation("Sent welcome card");
                    }
                    catch (Exception sendEx) when (sendEx.Message.Contains("Forbidden"))
                    {
                        ManualLogger.Log("[InstallUpdate] Warning: Received Forbidden during welcome card send, but likely already delivered.");
                        _logger.LogWarning("Forbidden on card send, but likely already received.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inside OnInstallationUpdateActivityAsync");
                    ManualLogger.Log($"[InstallUpdate ERROR] {ex.Message}\n{ex.StackTrace}");
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Error: {ex.Message}"), cancellationToken);
                }
            }
            else
            {
                _logger.LogWarning("Installation was not in personal scope. Skipping.");
                ManualLogger.Log("[InstallUpdate] Skipped: not personal scope.");
            }
        }
    }
}
