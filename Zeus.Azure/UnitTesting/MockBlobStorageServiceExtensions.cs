using Zeus.Azure.Storage;
using Moq;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Framework
{
    public static class MockBlobStorageServiceExtensions
    {
        public static void SetupUploadBlob(this Mock<IBlobStorageService> mockBlobStorageService, Stream sourceStream, string outputFilename, bool replace = false, bool createContainerIfNoneExists = true, bool shouldFail = false)
        {
            if(!shouldFail)
            {
                var uploadedResultContext = new UploadedBlobContext()
                {
                    BlobUrl = "prefix://" + outputFilename,
                    BlobName = outputFilename
                };

                mockBlobStorageService.Setup(mock => mock.UploadFileToContainerAsync(
                    It.Is<Stream>(s => s == sourceStream),
                    It.Is<string>(s => s == outputFilename),
                    It.Is<bool>(r => r == replace),
                    It.Is<bool>(b => b == createContainerIfNoneExists)
                ))
                .ReturnsAsync(uploadedResultContext);
            }
            else
            {
                mockBlobStorageService.Setup(mock => mock.UploadFileToContainerAsync(
                    It.Is<Stream>(s => s == sourceStream),
                    It.Is<string>(s => s == outputFilename),
                    It.Is<bool>(r => r == replace), 
                    It.Is<bool>(b => b == createContainerIfNoneExists)
                ))
                .Throws(new Exception("Test Error"));
            }
        }

        public static void SetupUploadBlob(this Mock<IBlobStorageService> mockBlobStorageService, string outputFilename, bool replace = false, bool createContainerIfNoneExists = false, bool shouldFail = false)
        {
            if (!shouldFail)
            {
                var uploadedResultContext = new UploadedBlobContext()
                {
                    BlobUrl = "prefix://" + outputFilename,
                    BlobName = outputFilename
                };

                mockBlobStorageService.Setup(mock => mock.UploadFileToContainerAsync(
                    It.IsAny<Stream>(),
                    It.Is<string>(s => s == outputFilename),
                    It.Is<bool>(r => r == replace),
                    It.Is<bool>(b => b == createContainerIfNoneExists)
                ))
                .ReturnsAsync(uploadedResultContext);
            }
            else
            {
                mockBlobStorageService.Setup(mock => mock.UploadFileToContainerAsync(
                    It.IsAny<Stream>(),
                    It.Is<string>(s => s == outputFilename),
                    It.Is<bool>(r => r == replace),
                    It.Is<bool>(b => b == false)
                ))
                .Throws(new Exception("Test Error"));
            }
        }

        public static void SetupUploadBlobWithCallback(this Mock<IBlobStorageService> mockBlobStorageService, Action<Stream> callback, string outputFilename, bool replace = false, bool createContainerIfNoneExists = false)
        {
            var uploadedResultContext = new UploadedBlobContext()
            {
                BlobUrl = "prefix://" + outputFilename,
                BlobName = outputFilename
            };

            mockBlobStorageService.Setup(mock => mock.UploadFileToContainerAsync(
                It.IsAny<Stream>(),
                It.Is<string>(s => s == outputFilename),
                It.Is<bool>(r => r == replace),
                It.Is<bool>(b => b == createContainerIfNoneExists)
            ))
            .Callback<Stream, string, bool, bool>((stream, o, r, c) => callback(stream))
            .Returns(Task.FromResult(uploadedResultContext));
        }

        public static void VerifyUploadBlob(this Mock<IBlobStorageService> mockBlobStorageService, Stream sourceStream)
        {
            mockBlobStorageService.Verify(mock => mock.UploadFileToContainerAsync(
                It.Is<Stream>(s => s == sourceStream),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.Is<bool>(b => b == false)
            ));
        }

        public static void VerifyUploadBlob(this Mock<IBlobStorageService> mockBlobStorageService, Stream sourceStream, string outputFilename, bool replace = false)
        {
            mockBlobStorageService.Verify(mock => mock.UploadFileToContainerAsync(
                It.Is<Stream>(s => s == sourceStream),
                It.Is<string>(s => s == outputFilename),
                It.Is<bool>(r => r == replace),
                It.Is<bool>(b => b == false)
            ));
        }

        public static void VerifyUploadBlob(this Mock<IBlobStorageService> mockBlobStorageService, string outputFilename, bool replace = false, bool createContainerIfNoneExists = false)
        {
            mockBlobStorageService.Verify(mock => mock.UploadFileToContainerAsync(
                It.IsAny<Stream>(),
                It.Is<string>(s => s == outputFilename),
                It.Is<bool>(r => r == replace),
                It.Is<bool>(b => b == createContainerIfNoneExists)
            ));
        }

        public static void SetupListAllBlobs(this Mock<IBlobStorageService> mockBlobStorageService, string[] files, string prefix)
        {
            mockBlobStorageService.Setup(mock => mock.ListAllBlobs(
                It.Is<string>(s => s == prefix)
            ))
            .ReturnsAsync(files);
        }

        public static void VerifyDownloadBlob(this Mock<IBlobStorageService> mockBlobStorageService, string file)
        {
            mockBlobStorageService.Verify(mock => mock.DownloadBlob(It.Is<string>(f => f == file)));
        }

        public static void SetupDownloadBlob(this Mock<IBlobStorageService> mockBlobStorageService, string fileName, string content)
        {
            mockBlobStorageService.Setup(mock => mock.DownloadBlob(It.Is<string>(s => s == fileName)))
                .ReturnsAsync(() =>
                {
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
                    writer.Write(content);
                    writer.Flush();

                    stream.Seek(0, SeekOrigin.Begin);
                    return stream;
                });
        }

        public static void SetupDownloadBlob(this Mock<IBlobStorageService> mockBlobStorageService, string fileName, Stream stream, bool blobExists = true)
        {
            if (blobExists)
            {
                mockBlobStorageService.Setup(m => m.DownloadBlob(
                    It.Is<string>(s => s == fileName)
                ))
                .ReturnsAsync(stream);
            }
            else
            {
                mockBlobStorageService.Setup(mock => mock.DownloadBlob(
                    It.Is<string>(f => f == fileName)
                ))
                .Throws(new BlobDoesNotExistException());
            }
        }


        public static void SetupDeleteBlob(this Mock<IBlobStorageService> mockBlobStorageService, string blobName, bool blobExists = true)
        {
            if(blobExists)
            {
                mockBlobStorageService.Setup(mock => mock.DeleteBlob(It.Is<string>(s => s == blobName)))
                    .Returns(Task.FromResult(0));
            }
            else
            {
                mockBlobStorageService.Setup(mock => mock.DeleteBlob(It.Is<string>(s => s == blobName)))
                    .Throws(new BlobDoesNotExistException());
            }
        }

        public static void VerifyDeleteBlob(this Mock<IBlobStorageService> mockBlobStorageService, string blobName)
        {
            mockBlobStorageService.Verify(m => m.DeleteBlob(It.Is<string>(s => s == blobName)));
        }

        public static void VerifyListAllBlobs(this Mock<IBlobStorageService> mockBlobStorageService, string prefix = null)
        {
            mockBlobStorageService.Verify(m => m.ListAllBlobs(
                It.Is<string>(p => p == prefix)
            ));
        }
    }
}
