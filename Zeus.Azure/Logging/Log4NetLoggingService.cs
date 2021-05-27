using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Logging
{
    public class Log4NetLoggingService : ILoggingService
    {
        private LoggingServiceConfiguration _configuration;
        private readonly ILog _log;

        public Log4NetLoggingService()
        {
            log4net.Config.XmlConfigurator.Configure();
            _log = log4net.LogManager.GetLogger("Main");
        }

        public void Configure(LoggingServiceConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void LogInfo(string message)
        {
            _log.Info(FormatMessage(message));
        }

        public void LogError(string message)
        {
            var e = new Exception();
            _log.Error(FormatMessage(message), e);
        }

        public void LogError(string message, Exception e)
        {
            _log.Error(FormatMessage(message), e);
        }

        public void LogError(Exception e)
        {
            _log.Error(FormatMessage(e.Message), e);
        }

        private string FormatMessage(string message)
        {
            return string.Format(
                "{0} - {1}",
                _configuration.ClassName,
                message
            );
        }
    }
}
