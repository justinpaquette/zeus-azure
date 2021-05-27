using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Storage
{
    public class StorageQueueProcessingServiceFactoryConfiguration
    {
        public string StorageAccountConnectionString { get; set; }
        public string QueueName { get; set; }
        public long IdleWaitTimeInMilliseconds { get; set; }
        public long VisibilityTimeoutInSeconds { get; set; }
    }

    public class StorageQueueProcessingServiceFactory<T> : IStorageQueueProcessingServiceFactory<T>
    {
        private readonly StorageQueueProcessingServiceFactoryConfiguration _configuration;
        private readonly ILoggingServiceFactory _loggingServiceFactory;

        public StorageQueueProcessingServiceFactory(
            StorageQueueProcessingServiceFactoryConfiguration configuration,
            ILoggingServiceFactory loggingServiceFactory
        )
        {
            _configuration = configuration;
            _loggingServiceFactory = loggingServiceFactory;
        }

        public IStorageQueueProcessingService<T> CreateStorageQueueProcessingService()
        {
            var storageQueueConfiguration = new StorageQueueConfiguration()
            {
                StorageAccountConnectionString = _configuration.StorageAccountConnectionString,
                QueueName = _configuration.QueueName
            };

            var storageQueue = new StorageQueue<T>(storageQueueConfiguration);

            var storageQueueProcessingServiceConfiguration = new StorageQueueProcessingServiceConfiguration()
            {
                IdleWaitTimeInMilliseconds = _configuration.IdleWaitTimeInMilliseconds,
                VisibilityTimeoutInSeconds = _configuration.VisibilityTimeoutInSeconds
            };

            var storageQueueProcessingService = new StorageQueueProcessingService<T>(
                storageQueueProcessingServiceConfiguration,
                _loggingServiceFactory.CreateLoggingService(),
                storageQueue
            );

            return storageQueueProcessingService;
        }
    }
}