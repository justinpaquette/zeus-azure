using Zeus.Azure.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Azure.Storage
{
    public class TableStorageService<T> : Service, ITableStorageService<T>
    {
        private readonly TableStorageServiceConfiguration _configuration;

        public TableStorageService(
            TableStorageServiceConfiguration configuration,
            ILoggingService loggingService
        )
            : base(loggingService)
        {
            _configuration = configuration;
        }

        public async Task AddEntry(ITableStorageEntry<T> entry)
        {
            var table = await GetCloudTable();

            var jsonEntity = SerializeEntryContent(entry);
            var operation = TableOperation.Insert(jsonEntity);

            await table.ExecuteAsync(operation);
        }

        private async Task<CloudTable> GetCloudTable()
        {
            var storageAccount = CloudStorageAccount.Parse(_configuration.StorageAccountConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(_configuration.TableName);

            var exists = await table.ExistsAsync();
            if (!exists)
            {
                await table.CreateAsync();
            }

            return table;
        }

        public async Task<T[]> GetEntriesForPrimaryIndex(string primaryIndex)
        {
            var table = await GetCloudTable();

            var query = new TableQuery<JsonTableStorageEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, primaryIndex));

            var entities = await table.ExecuteQueryAsync(query);

            return entities.Select(e => DeserializeEntry(e))
                .ToArray();
        }

        private JsonTableStorageEntity SerializeEntryContent(ITableStorageEntry<T> entry)
        {
            var jsonEntity = new JsonTableStorageEntity(entry.PrimaryIndex, entry.SecondaryIndex);
            jsonEntity.JsonContent = JsonConvert.SerializeObject(entry.Content);

            return jsonEntity;
        }

        private T DeserializeEntry(JsonTableStorageEntity jsonEntity)
        {
            return JsonConvert.DeserializeObject<T>(jsonEntity.JsonContent);
        }

        public IEnumerable<string> GetPartitionKeys()
        {
            var table = GetCloudTable().GetAwaiter().GetResult();

            var token = default(TableContinuationToken);

            var partitionKeys = new List<string>();

            do
            {
                var task = Task.Run(async () => await table.ExecuteQuerySegmentedAsync(new TableQuery<JsonTableStorageEntity>(), token));
                Task.WaitAll(task);

                var queryResult = task.Result;

                token = queryResult.ContinuationToken;

                foreach (var result in queryResult.Results)
                {
                    if (!partitionKeys.Contains(result.PartitionKey))
                    {
                        yield return result.PartitionKey;
                        partitionKeys.Add(result.PartitionKey);
                    }
                }
            } while (token != null);
        }
    }

    public class TableStorageServiceConfiguration
    {
        public string StorageAccountConnectionString { get; set; }
        public string TableName { get; set; }
    }

    public static class CloudTableExtensions
    {
        public static async Task<T[]> ExecuteQueryAsync<T>(this CloudTable table, TableQuery<T> query, CancellationToken ct = default(CancellationToken), Action<IList<T>> onProgress = null)
            where T : ITableEntity, new()
        {

            var items = new List<T>();
            TableContinuationToken token = null;

            do
            {

                TableQuerySegment<T> seg = await table.ExecuteQuerySegmentedAsync<T>(query, token);
                token = seg.ContinuationToken;
                items.AddRange(seg);
                if (onProgress != null) onProgress(items);

            } while (token != null && !ct.IsCancellationRequested);

            return items.ToArray();
        }
    }
}