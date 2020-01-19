using System;
using System.Collections.Generic;
using System.Text;
using KspNalCommon;
namespace KspCraftOrganizerPluginTests
{
    class PluginLoggerMock : IPluginLogger
    {
        public bool debug { get { return true;  } }

        public void logDebug(object toLog)
        {
            Console.WriteLine(toLog);
        }

        public void logError(string toLog)
        {
            Console.WriteLine(toLog);
        }

        public void logError(string toLog, Exception ex)
        {
            Console.WriteLine(toLog);
        }

        public void logTrace(object toLog)
        {
            Console.WriteLine(toLog);
        }
    }
}
