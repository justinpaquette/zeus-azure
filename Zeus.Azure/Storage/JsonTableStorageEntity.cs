using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Storage
{
    public class JsonTableStorageEntity : TableEntity
    {
        public string JsonContent { get; set; }

        public JsonTableStorageEntity()
        {

        }

        public JsonTableStorageEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }
    }
}