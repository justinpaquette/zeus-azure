using Zeus.Azure.Storage;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Tests.Storage
{
    public class CompressedBlobStorageServiceTests
    {
        private const string _prefix = "prefix";
        private const string _compressedExtension = ".compressed";

        private const string _testBlobPath = @"path2/path2/file1.ext";
        private const string _testBlobName = @"file1.ext";

        private const string _compressedBlobPath = @"path2/path2/file1.ext" + _compressedExtension;

        private const bool _uploadReplace = true;
        private const bool _uploadCreateContainerIfNotExists = true;

        private class CompressedBlobStorageServiceTestContext
        {
            public CompressedBlobStorageService SUT { get; set; }

            public Mock<ILoggingService> MockLoggingService { get; set; }
            public Mock<IBlobStorageService> MockBlobStorageService { get; set; }
            public Mock<IStreamCompressionService> MockStreamCompressionService { get; set; }

            public string[] BlobPaths { get; set; }

            public string StreamContent { get; set; }
            public string CompressedContent { get; set; }

            public Stream Stream { get; set; }
            public Stream CompressedStream { get; set; }
        }

        private CompressedBlobStorageServiceTestContext GetTestContext()
        {
            var configuration = new CompressedBlobStorageServiceConfiguration()
            {
                CompressedExtension = _compressedExtension
            };

            var context = new CompressedBlobStorageServiceTestContext()
            {
                MockLoggingService = new Mock<ILoggingService>(),
                MockBlobStorageService = new Mock<IBlobStorageService>(),
                MockStreamCompressionService = new Mock<IStreamCompressionService>(),

                BlobPaths = GetAllBlobPaths(),
                StreamContent = GetStreamContent(),
                CompressedContent = GetStreamContent()
            };

            context.Stream = GetStream(context.StreamContent);
            context.CompressedStream = GetStream(context.CompressedContent);

            SetupMockStreamCompressionService(context);

            context.SUT = new CompressedBlobStorageService(
                configuration,
                context.MockLoggingService.Object,
                context.MockBlobStorageService.Object,
                context.MockStreamCompressionService.Object
            );

            return context;
        }

        private void SetupMockStreamCompressionService(CompressedBlobStorageServiceTestContext context)
        {
            context.MockStreamCompressionService.SetupCompressStream(
                context.Stream,
                _testBlobName,
                context.CompressedStream
            );

            context.MockStreamCompressionService.SetupDeCompressStream(
                context.CompressedStream,
                context.Stream
            );
        }

        [Test]
        public async Task PassThroughPrefixWhenListingBlobs()
        {
            //Arrange
            var context = GetTestContext();

            //Act
            var result = await context.SUT.ListAllBlobs(_prefix);

            //Assert
            context.MockBlobStorageService.VerifyListAllBlobs(_prefix);
        }

        [Test]
        public async Task RemoveCompressedExtensionsWhenListingBlobs()
        {
            //Arrange
            var context = GetTestContext();

            context.MockBlobStorageService.SetupListAllBlobs(context.BlobPaths, _prefix);

            //Act
            var result = await context.SUT.ListAllBlobs(_prefix);

            //Assert
            var expectedBlobs = GetAllBlobPathsWithoutCompressedExtensions(context.BlobPaths);

            Assert.IsTrue(
                result.SequenceEqual(expectedBlobs)
            );
        }

        [Test]
        public async Task DeleteCompressedVersionOfBlob()
        {
            //Arrange
            var context = GetTestContext();

            var compressedBlobPath = _testBlobPath + _compressedExtension;
            context.MockBlobStorageService.SetupDeleteBlob(compressedBlobPath);

            //Act
            await context.SUT.DeleteBlob(_testBlobPath);

            //Assert
            context.MockBlobStorageService.VerifyDeleteBlob(compressedBlobPath);
        }

        [Test]
        public async Task DeleteNonCompressedVersionOfBlobIfCompressedVersionDoesNotExist()
        {
            //Arrange
            var context = GetTestContext();

            var compressedBlobPath = _testBlobPath + _compressedExtension;
            context.MockBlobStorageService.SetupDeleteBlob(compressedBlobPath, false);
            context.MockBlobStorageService.SetupDeleteBlob(_testBlobPath, true);

            //Act
            await context.SUT.DeleteBlob(_testBlobPath);

            //Assert
            context.MockBlobStorageService.VerifyDeleteBlob(compressedBlobPath);
            context.MockBlobStorageService.VerifyDeleteBlob(_testBlobPath);
        }

        [Test]
        public async Task ThrowIfDeletingBlobAndBothCompressedAndNonCompressedVersionDoNotExist()
        {
            //Arrange
            var context = GetTestContext();

            var compressedBlobPath = _testBlobPath + _compressedExtension;
            context.MockBlobStorageService.SetupDeleteBlob(compressedBlobPath, false);
            context.MockBlobStorageService.SetupDeleteBlob(_testBlobPath, false);

            //Act
            var exception = default(BlobDoesNotExistException);

            try
            {
                await context.SUT.DeleteBlob(_testBlobPath);
            }
            catch (BlobDoesNotExistException e)
            {
                exception = e;
            }

            //Assert
            Assert.IsNotNull(exception);
        }

        [Test]
        public async Task UploadCompressedVersionOfBlob()
        {
            //Arrange
            var context = GetTestContext();

            //Act
            await context.SUT.UploadFileToContainerAsync(
                context.Stream,
                _testBlobPath
            );

            //Assert
            context.MockStreamCompressionService.VerifyCompressStream(context.Stream, _testBlobName);
            context.MockBlobStorageService.VerifyUploadBlob(context.CompressedStream, _compressedBlobPath);
            context.MockBlobStorageService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task DownloadCompressedBlobIfExistsAndDeCompress()
        {
            //Arrange
            var context = GetTestContext();

            context.MockBlobStorageService.SetupDownloadBlob(_compressedBlobPath, context.CompressedStream, true);

            //Act
            var result = await context.SUT.DownloadBlob(_testBlobPath);

            //Assert
            Assert.IsTrue(result == context.Stream);
            context.MockStreamCompressionService.VerifyDeCompressStream(context.CompressedStream);
        }

        [Test]
        public async Task DownloadNonCompressedVersionIfCompressedVersionDoesNotExist()
        {
            //Arrange
            var context = GetTestContext();

            context.MockBlobStorageService.SetupDownloadBlob(_compressedBlobPath, context.CompressedStream, false);
            context.MockBlobStorageService.SetupDownloadBlob(_testBlobPath, context.Stream, true);

            //Act
            var result = await context.SUT.DownloadBlob(_testBlobPath);

            //Assert
            Assert.IsTrue(
                result == context.Stream
            );
        }

        [Test]
        public async Task ThrowExceptionIfBothCompressedAndNonCompressedFilesDoNotExist()
        {
            //Arrange
            var context = GetTestContext();

            context.MockBlobStorageService.SetupDownloadBlob(_compressedBlobPath, context.CompressedStream, false);
            context.MockBlobStorageService.SetupDownloadBlob(_testBlobPath, context.Stream, false);

            //Act
            var exception = default(BlobDoesNotExistException);

            try
            {
                var result = await context.SUT.DownloadBlob(_testBlobPath);
            }
            catch (BlobDoesNotExistException e)
            {
                exception = e;
            }

            //Assert
            Assert.IsNotNull(exception);
        }

        private string[] GetAllBlobPaths()
        {
            return new[]
            {
                @"path1/path2/file1.ext",
                @"path1/path2/file1.ext" + _compressedExtension,
                @"file2.ext",
                @"file3.ext" + _compressedExtension,
                @"file4.ext",
                @"file5.ext",
                @"path1/file1.ext",
                @"path1/file2.ext" + _compressedExtension
            };
        }

        private string[] GetAllBlobPathsWithoutCompressedExtensions(string[] blobPaths)
        {
            return blobPaths
                .Select(b => b.Replace(_compressedExtension, string.Empty))
                .ToArray();
        }


        private Stream GetStream(string content)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        private string GetStreamContent()
        {
            var random = new Random();
            return random.NextString(Int16.MaxValue);
        }

        private bool CompareStreamContent(Stream stream, string content)
        {
            var reader = new StreamReader(stream);
            var readContent = reader.ReadToEnd();

            return content == readContent;
        }
    }
}