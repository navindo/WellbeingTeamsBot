using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.IO;

namespace WellbeingTeamsBot.Services
{
    public static class ManualLogger
    {
        private static bool _enabled;
        private static readonly string _logFolderPath = "/home/LogFiles";

        public static void Initialize(IConfiguration config)
        {
            _enabled = string.Equals(config["EnableManualLog"], "true", StringComparison.OrdinalIgnoreCase);
        }

        public static void Log(string message)
        {
            if (!_enabled) return;

            try
            {
                string date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string filePath = Path.Combine(_logFolderPath, $"manual-log-{date}.txt");

                string logEntry = $"[{DateTime.UtcNow:u}] {message}{Environment.NewLine}";
                File.AppendAllText(filePath, logEntry);
            }
            catch
            {
                // silently fail
            }
        }
    }
}
