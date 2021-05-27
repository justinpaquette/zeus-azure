using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure
{
    public interface IStorageQueue<T>
    {
        Task<IStorageQueueMessage<T>> GetMessage(TimeSpan visibilityTimeout);
        Task QueueMessage(T message);
        Task DeleteMessage(IStorageQueueMessage<T> storageQueueMessage);
        Task RenewMessage(IStorageQueueMessage<T> storageQueueMessage, TimeSpan visiblityTimeout);
    }

    public interface IStorageQueueMessage<T>
    {
        object CloudQueueMessage { get; set; }
        T Message { get; set; }
    }
}
