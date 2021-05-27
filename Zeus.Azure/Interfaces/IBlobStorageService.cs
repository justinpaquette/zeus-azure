using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure
{
    public interface IBlobStorageService
    {
        Task<UploadedBlobContext> UploadFileToContainerAsync(Stream sourceStream, string outputFilename, bool replace = false, bool createContainerIfNotExists = false);
        Task<string[]> ListAllBlobs(string prefix = null);
        Task<Stream> DownloadBlob(string blobName);
        IEnumerable<Stream> DownloadBlobs(string[] blobs);
        Task DeleteBlob(string blobName);
    }

    public class UploadedBlobContext
    {
        public string BlobUrl { get; set; }
        public string BlobName { get; set; }
    }

}
