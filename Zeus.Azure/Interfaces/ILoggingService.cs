using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure
{
    public interface ILoggingService
    {
        void Configure(LoggingServiceConfiguration configuration);

        void LogInfo(string message);

        void LogError(string message);

        void LogError(string message, Exception e);

        void LogError(Exception e);
    }

    public class LoggingServiceConfiguration
    {
        public string ClassName { get; set; }
    }


}