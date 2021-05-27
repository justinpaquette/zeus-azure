using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Logging
{
    public class Log4NetLoggingServiceFactory : ILoggingServiceFactory
    {
        public ILoggingService CreateLoggingService()
        {
            return new Log4NetLoggingService();
        }
    }
}
