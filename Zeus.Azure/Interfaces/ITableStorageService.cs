using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure
{
    public interface ITableStorageService<T>
    {
        Task AddEntry(ITableStorageEntry<T> entry);
        Task<T[]> GetEntriesForPrimaryIndex(string primaryIndex);
        IEnumerable<string> GetPartitionKeys();
    }

    public interface ITableStorageEntry<T>
    {
        string PrimaryIndex { get; set; }
        string SecondaryIndex { get; set; }
        T Content { get; set; }
    }
}