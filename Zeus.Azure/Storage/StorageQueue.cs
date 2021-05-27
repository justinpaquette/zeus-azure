using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Storage
{
    public class StorageQueue<T> : IStorageQueue<T>
    {
        private readonly StorageQueueConfiguration _configuration;
        private readonly CloudQueue _queue;

        public StorageQueue(
            StorageQueueConfiguration configuration
        )
        {
            _configuration = configuration;

            _queue = GetCloudQueueReference().GetAwaiter().GetResult();
        }

        public async Task<IStorageQueueMessage<T>> GetMessage(TimeSpan visibilityTimeout)
        {
            var options = new QueueRequestOptions();

            var cloudQueueMessage = await _queue.GetMessageAsync(visibilityTimeout, options, null);

            if (cloudQueueMessage == null)
            {
                return null;
            }

            var messageContentJson = cloudQueueMessage.AsString;

            var message = Deserialize(messageContentJson);

            return new StorageQueueMessage<T>()
            {
                CloudQueueMessage = cloudQueueMessage,
                Message = message
            };
        }

        public async Task QueueMessage(T message)
        {
            var messageJson = Serialize(message);
            var cloudQueueMessage = new CloudQueueMessage(messageJson);

            await _queue.AddMessageAsync(cloudQueueMessage);
        }

        public async Task DeleteMessage(IStorageQueueMessage<T> message)
        {
            var cloudQueueMessage = message.CloudQueueMessage as CloudQueueMessage;

            if (cloudQueueMessage == null)
            {
                throw new ArgumentException("Invalid cloud queue message type");
            }

            await _queue.DeleteMessageAsync(cloudQueueMessage);
        }

        public async Task RenewMessage(IStorageQueueMessage<T> storageQueueMessage, TimeSpan visiblityTimeout)
        {
            var cloudQueueMessage = storageQueueMessage.CloudQueueMessage as CloudQueueMessage;

            if (cloudQueueMessage == null)
            {
                throw new ArgumentException("Invalid cloud queue message type");
            }

            await _queue.UpdateMessageAsync(cloudQueueMessage, visiblityTimeout, MessageUpdateFields.Visibility);
        }

        private T Deserialize(string message)
        {
            return JsonConvert.DeserializeObject<T>(message);
        }

        private string Serialize(T message)
        {
            return JsonConvert.SerializeObject(message);
        }

        private async Task<CloudQueue> GetCloudQueueReference()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                _configuration.StorageAccountConnectionString
            );

            var queueClient = storageAccount.CreateCloudQueueClient();

            var queue = queueClient.GetQueueReference(_configuration.QueueName);

            await queue.CreateIfNotExistsAsync();

            return queue;
        }
    }

    public class StorageQueueConfiguration
    {
        public string StorageAccountConnectionString { get; set; }
        public string QueueName { get; set; }
    }

}