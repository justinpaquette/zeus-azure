using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Services
{
    public abstract class Service
    {
        private readonly ILoggingService _loggingService;

        protected ILoggingService LogService
        {
            get
            {
                return _loggingService;
            }
        }

        public Service(ILoggingService loggingService)
        {
            _loggingService = loggingService;

            _loggingService.Configure(
                new LoggingServiceConfiguration()
                {
                    ClassName = this.GetType().Name.Split('.').Last()
                });
        }
    }
}