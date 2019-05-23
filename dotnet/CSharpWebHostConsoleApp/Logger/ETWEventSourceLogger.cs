using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Session;

namespace Logger
{

    [EventSource(Name = EventSourceName)]
    sealed class ETWEventSourceLogger : EventSource, ILogger, IDisposable
    {
        public const string EventSourceName = "CalcServiceLogger";
        private LogLevel minLogLevel;
        private static readonly object Lockobj = new object();
        private static ETWEventSourceLogger instance;

        private static void Initialize()
        {
            lock (Lockobj)
            {
                if (instance == null)
                {
                    instance = new ETWEventSourceLogger();
                }
            }
        }

        public ILogger GetInstance()
        {
            if (instance == null)
            {
                Initialize();
            }

            return instance;
        }

        private ETWEventSourceLogger()
        {
            var session = new TraceEventSession("CalcCommonLogger");
            session.EnableProvider(EventSourceName);
        }

        public void Log(LogLevel logLevel, string msg, params object[] args)
        {
            WriteEvent(1, msg);
        }
    }
}
