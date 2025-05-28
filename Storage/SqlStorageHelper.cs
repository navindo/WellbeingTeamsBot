using Microsoft.Bot.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Threading.Tasks;
using WellbeingTeamsBot.Services;

namespace WellbeingTeamsBot.Storage
{
    public class SqlStorageHelper : ISqlStorageHelper
    {
        private readonly string _connectionString;

        public SqlStorageHelper(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("BotDb");
        }

        public async Task StoreOrUpdateReferenceAsync(string objectId, ConversationReference reference)
        {
            var referenceJson = JsonConvert.SerializeObject(reference);

            var sql = @"
MERGE dbo.UserConversationReference AS target
USING (SELECT @ObjectId AS ObjectId) AS source
ON target.ObjectId = source.ObjectId
WHEN MATCHED THEN 
    UPDATE SET ConversationReferenceJson = @ReferenceJson, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (ObjectId, ConversationReferenceJson, NotificationsEnabled, UpdatedAt) 
    VALUES (@ObjectId, @ReferenceJson, 1, SYSUTCDATETIME());";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ObjectId", objectId);
                cmd.Parameters.AddWithValue("@ReferenceJson", referenceJson);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                ManualLogger.Log($"[StoreOrUpdateReferenceAsync] objectId={objectId}, Error={ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<ConversationReference> GetReferenceAsync(string objectId)
        {
            var sql = "SELECT ConversationReferenceJson FROM dbo.UserConversationReference WHERE ObjectId = @ObjectId";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ObjectId", objectId);
                await conn.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();
                return result != null
                    ? JsonConvert.DeserializeObject<ConversationReference>(result.ToString())
                    : null;
            }
            catch (Exception ex)
            {
                ManualLogger.Log($"[GetReferenceAsync] objectId={objectId}, Error={ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task UpdateNotificationStatusAsync(string objectId, bool enabled, DateTime? snoozedUntilUtc)
        {
            var sql = @"
UPDATE dbo.UserConversationReference
SET NotificationsEnabled = @Enabled,
    SnoozedUntilUtc = @SnoozedUntil,
    UpdatedAt = SYSUTCDATETIME()
WHERE ObjectId = @ObjectId";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ObjectId", objectId);
                cmd.Parameters.AddWithValue("@Enabled", enabled);
                cmd.Parameters.AddWithValue("@SnoozedUntil", (object?)snoozedUntilUtc ?? DBNull.Value);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                ManualLogger.Log($"[UpdateNotificationStatusAsync] objectId={objectId}, Error={ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<(bool Enabled, DateTime? SnoozedUntilUtc)> GetNotificationStatusAsync(string objectId)
        {
            var sql = @"
SELECT NotificationsEnabled, SnoozedUntilUtc
FROM dbo.UserConversationReference
WHERE ObjectId = @ObjectId";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ObjectId", objectId);
                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var enabled = reader.GetBoolean(0);
                    var snoozedUntil = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                    return (enabled, snoozedUntil);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                ManualLogger.Log($"[GetNotificationStatusAsync] objectId={objectId}, Error={ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task UpdateLastAlertSentAsync(string objectId)
        {
            var sql = @"
UPDATE dbo.UserConversationReference
SET LastAlertSent = SYSUTCDATETIME(),
    UpdatedAt = SYSUTCDATETIME()
WHERE ObjectId = @ObjectId";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ObjectId", objectId);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                ManualLogger.Log($"[UpdateLastAlertSentAsync] objectId={objectId}, Error={ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
    }
}
