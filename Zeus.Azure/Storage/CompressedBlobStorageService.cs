using Zeus.Azure.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Storage
{
    public class CompressedBlobStorageService : Service, ICompressedBlobStorageService
    {
        private readonly CompressedBlobStorageServiceConfiguration _configuration;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IStreamCompressionService _streamCompressionService;

        public CompressedBlobStorageService(
            CompressedBlobStorageServiceConfiguration configuration,
            ILoggingService loggingService,
            IBlobStorageService blobStorageService,
            IStreamCompressionService streamCompressionService
        ) 
            : base(loggingService)
        {
            _configuration = configuration;
            _blobStorageService = blobStorageService;
            _streamCompressionService = streamCompressionService;
        }

        public async Task DeleteBlob(string blobName)
        {
            var compressedBlobName = GetCompressedBlobName(blobName);

            try
            {
                await _blobStorageService.DeleteBlob(compressedBlobName);
            }
            catch(BlobDoesNotExistException)
            {
                await _blobStorageService.DeleteBlob(blobName);
            }
        }

        private string GetCompressedBlobName(string blobName)
        {
            return blobName + _configuration.CompressedExtension;
        }

        public async Task<Stream> DownloadBlob(string blobName)
        {
            var compressedBlobName = GetCompressedBlobName(blobName);

            var stream = default(Stream);

            try
            {
                using (var zippedStream = await _blobStorageService.DownloadBlob(compressedBlobName))
                {
                    stream = _streamCompressionService.DeCompressStream(zippedStream);
                }
            }
            catch(BlobDoesNotExistException)
            {
                stream = await _blobStorageService.DownloadBlob(blobName);
            }

            return stream;
        }

        public IEnumerable<Stream> DownloadBlobs(string[] blobs)
        {
            foreach (var blob in blobs)
            {
                var task = DownloadBlob(blob);
                task.Wait();
                yield return task.Result;
            }
        }

        public async Task<string[]> ListAllBlobs(string prefix = null)
        {
            var blobList = await _blobStorageService.ListAllBlobs(prefix);

            return blobList.Select(blobName =>
            {
                if (blobName.EndsWith(_configuration.CompressedExtension))
                {
                    return blobName.Replace(_configuration.CompressedExtension, string.Empty);
                }
                else
                {
                    return blobName;
                }
            })
            .ToArray();
        }

        public async Task<UploadedBlobContext> UploadFileToContainerAsync(Stream sourceStream, string blobName, bool replace = false, bool createContainerIfNotExists = false)
        {
            var compressedBlobName = GetCompressedBlobName(blobName);

            using (var zippedStream = _streamCompressionService.CompressStream(sourceStream, GetFileName(blobName)))
            {
                return await _blobStorageService.UploadFileToContainerAsync(
                    zippedStream, 
                    compressedBlobName, 
                    replace, 
                    createContainerIfNotExists
                );
            }
        }

        private string GetFileName(string blobName)
        {
            return blobName.Split('/').Last();
        }
    }

    public class CompressedBlobStorageServiceConfiguration
    {
        public string CompressedExtension { get; set; }
    }
}
