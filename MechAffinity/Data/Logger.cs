using System;
using System.IO;

namespace MechAffinity.Data
{
    struct Logger
    {
        private static StreamWriter logStream;

        public Logger(string modDir, string fileName)
        {
            string filePath = Path.Combine(modDir, $"{fileName}.log");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            logStream = File.AppendText(filePath);

        }

        public void Write(string message)
        {
            string now = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            logStream.WriteLine($"{now} - {message}");
            logStream.Flush();
        }
        
        public void Write(Exception error)
        {
            string now = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            logStream.WriteLine($"CRITICAL: {now} - {error}");
            logStream.Flush();
        }

    }
}
