using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    /// <summary>
    /// Naive logger implementation.
    /// </summary>
    class FileLogger : ILogger, IDisposable
    {
        private static readonly object Lockobj = new object();
        private readonly FileStream stream;
        private LogLevel minLogLevel;
        private static FileLogger instance;

        private static void Initialize()
        {
            lock (Lockobj)
            {
                if (instance == null)
                {
                    instance = new FileLogger();
                }
            }
        }


        static ILogger GetInstance()
        {
            if (instance == null)
            {
                Initialize();
            }

            return instance;
        }

        private FileLogger()
        {
            string logFolderPath =
                ConfigurationManager.AppSettings.Get("LogFolder");

            if (string.IsNullOrEmpty(logFolderPath))
            {
                logFolderPath = Path.GetTempPath();
            }

            // todo: derive the file name from current time
            string logFilePath = Path.Combine(logFolderPath, "tmp.log");

            this.stream = new FileStream(
                logFolderPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite);
        }

        public FileLogger(LogLevel minLogLevel)
        {
            this.minLogLevel = minLogLevel;
        }

        public void Log(LogLevel logLevel, string msg, params object[] args)
        {
            if (logLevel >= minLogLevel)
            {
                var bytes = Encoding.UTF8.GetBytes(string.Format(msg, args).ToCharArray());
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public void Dispose()
        {
            stream.Close();
        }
    }
}
