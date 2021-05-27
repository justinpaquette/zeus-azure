using Zeus.Azure.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Azure.Storage
{
    public class StorageQueueProcessingServiceConfiguration
    {
        public long IdleWaitTimeInMilliseconds { get; set; }
        public long VisibilityTimeoutInSeconds { get; set; }
    }

    public class StorageQueueProcessingService<T> : Service, IStorageQueueProcessingService<T>
    {
        private readonly StorageQueueProcessingServiceConfiguration _configuration;
        private readonly IStorageQueue<T> _storageQueue;

        public StorageQueueProcessingService(
            StorageQueueProcessingServiceConfiguration configuration,
            ILoggingService loggingService,
            IStorageQueue<T> storageQueue
        )
            : base(loggingService)
        {
            _configuration = configuration;
            _storageQueue = storageQueue;
        }

        public async Task ProcessQueue(Func<T, Task> onMessage, CancellationToken ct)
        {
            var visibilityTimeout = TimeSpan.FromSeconds(_configuration.VisibilityTimeoutInSeconds);

            while (!ct.IsCancellationRequested)
            {
                var message = await _storageQueue.GetMessage(visibilityTimeout);

                if (message != null)
                {
                    await ProcessMessage(onMessage, message);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(_configuration.IdleWaitTimeInMilliseconds));
                }
            }
        }

        public async Task ProcessMessage(Func<T, Task> onMessage, IStorageQueueMessage<T> message)
        {
            try
            {
                await ProcessMessageAndRenewUntilComplete(onMessage, message);
                await _storageQueue.DeleteMessage(message);
            }
            catch (Exception e)
            {
                this.LogService.LogError(
                    string.Format(
                        "Error processing queue message: {0}",
                        JsonConvert.SerializeObject(message.Message)
                    ),
                    e
                );
            }
        }

        public async Task ProcessMessageAndRenewUntilComplete(Func<T, Task> onMessage, IStorageQueueMessage<T> message)
        {
            var cts = new CancellationTokenSource();

            var renewMessageTask = RenewMessageOnInterval(message, cts.Token);

            await onMessage(message.Message);
            cts.Cancel();
        }

        public async Task RenewMessageOnInterval(IStorageQueueMessage<T> message, CancellationToken ct)
        {
            var timeoutInSeconds = (int)Math.Round(_configuration.VisibilityTimeoutInSeconds / 2d);
            var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

            var visibilityTimeout = TimeSpan.FromSeconds(_configuration.VisibilityTimeoutInSeconds);

            while (true)
            {
                await Task.Delay(timeout, ct);

                if (ct.IsCancellationRequested)
                {
                    break;
                }

                await _storageQueue.RenewMessage(message, visibilityTimeout);
            }
        }
    }
}