using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;

namespace Zeus.Azure.Services
{
    public class StreamCompressionService : Service, IStreamCompressionService
    {
        private readonly StreamCompressionServiceConfiguration _configuration;

        public StreamCompressionService(
            StreamCompressionServiceConfiguration configuration,
            ILoggingService loggingService
        ) 
            : base(loggingService)
        {
            _configuration = configuration;
        }

        public Stream CompressStream(Stream sourceStream, string fileName)
        {
            var streamEntryContexts = new[]
            {
                new CompressedFileEntryStreamContext()
                {
                    Name = fileName,
                    Stream = sourceStream
                }
            };

            return CompressStreams(streamEntryContexts);
        }

        private class CompressedFileEntryStreamContext
        {
            public Stream Stream { get; set; }
            public string Name { get; set; }
        }

        private Stream CompressStreams(CompressedFileEntryStreamContext[] compressedEntryContexts)
        {
            var compressedStream = new MemoryStream();

            using (var archive = new ZipArchive(compressedStream, ZipArchiveMode.Create, true))
            {
                foreach (var streamEntryContext in compressedEntryContexts)
                {
                    var entry = archive.CreateEntry(streamEntryContext.Name, _configuration.CompressionLevel);

                    using (var entryStream = entry.Open())
                    {
                        streamEntryContext.Stream.CopyTo(entryStream);
                    }
                }
            }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        public Stream DeCompressStream(Stream compressedStream)
        {
            using (var archive = new ZipArchive(compressedStream, ZipArchiveMode.Read, true))
            {
                var entry = archive.Entries.First();
                using (var entryStream = entry.Open())
                {
                    var returnStream = new MemoryStream();
                    entryStream.CopyTo(returnStream);

                    returnStream.Seek(0, SeekOrigin.Begin);
                    return returnStream;
                }
            }
        }
    }

    public class StreamCompressionServiceConfiguration
    {
        public CompressionLevel CompressionLevel { get; set; }
    }
}
