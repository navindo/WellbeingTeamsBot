using Microsoft.Bot.Schema;
using System;
using System.Threading.Tasks;

namespace WellbeingTeamsBot.Storage
{
    public interface ISqlStorageHelper
    {
        Task StoreOrUpdateReferenceAsync(string objectId, ConversationReference reference);
        Task<ConversationReference> GetReferenceAsync(string objectId);
        Task UpdateNotificationStatusAsync(string objectId, bool enabled, DateTime? snoozedUntilUtc);
        Task<(bool Enabled, DateTime? SnoozedUntilUtc)> GetNotificationStatusAsync(string objectId);
        Task UpdateLastAlertSentAsync(string objectId);
    }
}
