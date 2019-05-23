using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    enum LogLevel
    {
        Trace, Info, Error
    }

    interface  ILogger
    {
        void Log(LogLevel logLevel, string msg, params object[] args);
    }
}
