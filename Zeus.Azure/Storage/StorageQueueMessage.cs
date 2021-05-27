using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Storage
{
    public class StorageQueueMessage<T> : IStorageQueueMessage<T>
    {
        public object CloudQueueMessage { get; set; }
        public T Message { get; set; }
    }
}