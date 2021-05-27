using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using Zeus.Azure.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Storage
{
    public class BlobStorageService : Service, IBlobStorageService
    {
        private readonly BlobStorageServiceConfiguration _configuration;

        public BlobStorageService(
            BlobStorageServiceConfiguration configuration,
            ILoggingService loggingService
        ) 
            : base(loggingService)
        {
            _configuration = configuration;
        }

        public async Task<string[]> ListAllBlobs(string prefix = null)
        {
            throw new NotImplementedException();

            //REFACTOR: Adapt to new segmented list async method

            //try
            //{
            //    var storageAccount = CloudStorageAccount.Parse(_configuration.StorageAccountConnectionString);
            //    var blobClient = storageAccount.CreateCloudBlobClient();
            //    var container = blobClient.GetContainerReference(_configuration.ContainerName);
                
            //    // Loop over items within the container and output the length and URI.
            //    return container.ListBlobs(prefix, true)
            //        .OfType<CloudBlockBlob>()
            //        .Select(b => b.Name)
            //        .ToArray();
            //}
            //catch (Exception e)
            //{
            //    this.LogService.LogError(e);
            //    throw e;
            //}
        }

        public async Task<UploadedBlobContext> UploadFileToContainerAsync(Stream sourceStream, string outputFilename, bool replace = false, bool createContainerIfNotExists = false)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(_configuration.StorageAccountConnectionString);

                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                var cloudBlobContainer = cloudBlobClient.GetContainerReference(_configuration.ContainerName);

                if(!(await cloudBlobContainer.ExistsAsync()))
                {
                    if(createContainerIfNotExists)
                    {
                        await cloudBlobContainer.CreateIfNotExistsAsync();
                    }
                    else
                    {
                        throw new ArgumentException("Error uploading file: Container does not exist");
                    }
                }

                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(outputFilename);


                if(await cloudBlockBlob.ExistsAsync() && !replace)
                {
                    throw new ArgumentException("Error uploading file: File already exists and replace flag not set to true");
                }

                sourceStream.Seek(0, SeekOrigin.Begin);
                await cloudBlockBlob.UploadFromStreamAsync(sourceStream);

                var resultContext = new UploadedBlobContext()
                {
                    BlobUrl = cloudBlockBlob.Uri.AbsoluteUri,
                    BlobName = outputFilename
                };

                this.LogService.LogInfo(
                    string.Format("Successfully uploaded blob:{0}", outputFilename)
                );

                return resultContext;
            }
            catch(Exception e)
            {
                this.LogService.LogError(
                    string.Format("Error uploading blob: {0}", outputFilename),
                    e
                );

                throw e;
            }
        }

        public async Task<Stream> DownloadBlob(string blobName)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(_configuration.StorageAccountConnectionString);

                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                var cloudBlobContainer = cloudBlobClient.GetContainerReference(_configuration.ContainerName);

                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);

                var exists = await cloudBlockBlob.ExistsAsync();
                if(!exists)
                {
                    throw new BlobDoesNotExistException();
                }

                var stream = new MemoryStream();
                await cloudBlockBlob.DownloadToStreamAsync(stream);

                this.LogService.LogInfo(
                    string.Format("Successfully downloaded blob:{0}", blobName)
                );

                stream.Seek(0, SeekOrigin.Begin);

                return stream;
            }
            catch(BlobDoesNotExistException e)
            {
                this.LogService.LogError(
                    string.Format("Error downloading blob, blob does not exist: {0}", blobName),
                    e
                );

                throw e;
            }
            catch (Exception e)
            {
                this.LogService.LogError(
                    string.Format("Error downloading blob: {0}", blobName),
                    e
                );

                throw new BlobDownloadException("Error downloading blob", e);
            }
        }

        public IEnumerable<Stream> DownloadBlobs(string[] blobs)
        {
            foreach(var blob in blobs)
            {
                var task = DownloadBlob(blob);
                task.Wait();
                yield return task.Result;
            }
        }

        public async Task DeleteBlob(string blobName)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(_configuration.StorageAccountConnectionString);

                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                var cloudBlobContainer = cloudBlobClient.GetContainerReference(_configuration.ContainerName);
                await cloudBlobContainer.CreateIfNotExistsAsync();
                
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);

                var exists = await cloudBlockBlob.ExistsAsync();
                if (!exists)
                {
                    throw new BlobDoesNotExistException();
                }

                await cloudBlockBlob.DeleteAsync();

                this.LogService.LogInfo(
                    string.Format("Successfully deleted blob:{0}", blobName)
                );
            }
            catch (BlobDoesNotExistException e)
            {
                this.LogService.LogError(
                    string.Format("Error deleting blob, blob does not exist: {0}", blobName),
                    e
                );

                throw e;
            }
            catch (Exception e)
            {
                this.LogService.LogError(
                    string.Format("Error deleting blob: {0}", blobName),
                    e
                );

                throw e;
            }
        }
    }

    public class BlobStorageServiceConfiguration
    {
        public string ContainerName { get; set; }
        public string StorageAccountConnectionString { get; set; }
    }
}
