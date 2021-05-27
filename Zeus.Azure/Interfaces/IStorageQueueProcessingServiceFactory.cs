using System;
using System.Collections.Generic;
using System.Text;

namespace Zeus.Azure
{
    public interface IStorageQueueProcessingServiceFactory<T>
    {
        IStorageQueueProcessingService<T> CreateStorageQueueProcessingService();
    }
}
