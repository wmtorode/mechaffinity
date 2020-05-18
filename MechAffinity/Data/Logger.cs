using System;
using System.IO;

namespace MechAffinity.Data
{
    class Logger
    {
        private static StreamWriter logStream;
        private bool enableDebug = false;

        public Logger(string modDir, string fileName, bool enableDebug)
        {
            string filePath = Path.Combine(modDir, $"{fileName}.log");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            logStream = File.AppendText(filePath);
            logStream.AutoFlush = true;

            this.enableDebug = enableDebug;
        }

        public void DebugMessage(string message)
        {
            if (enableDebug)
            {
                string now = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
                logStream.WriteLine($"DEBUG: {now} - {message}");
            }
        }

        public void LogMessage(string message)
        {
            string now = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            logStream.WriteLine($"INFO: {now} - {message}");
        }

        public void LogError(string message)
        {
            string now = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            logStream.WriteLine($"ERROR: {now} - {message}");
        }

        public void LogException(Exception error)
        {
            string now = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            logStream.WriteLine($"CRITICAL: {now} - {error}");
        }

    }
}
