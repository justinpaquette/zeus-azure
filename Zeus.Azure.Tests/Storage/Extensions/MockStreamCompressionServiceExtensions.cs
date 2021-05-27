using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure
{
    public static class MockStreamCompressionServiceExtensions
    {
        public static void SetupCompressStream(
            this Mock<IStreamCompressionService> mockStreamCompressionService,
            Stream sourceStream,
            string entryName,
            Stream returnStream
        )
        {
            mockStreamCompressionService.Setup(m => m.CompressStream(
                It.Is<Stream>(s => s == sourceStream),
                It.Is<string>(name => name == entryName)
            ))
            .Returns(returnStream);
        }

        public static void VerifyCompressStream(
            this Mock<IStreamCompressionService> mockStreamCompressionService,
            Stream sourceStream,
            string entryName
        )
        {
            mockStreamCompressionService.Verify(m => m.CompressStream(
                It.Is<Stream>(s => s == sourceStream),
                It.Is<string>(s => s == entryName)
            ));
        }

        public static void SetupDeCompressStream(
            this Mock<IStreamCompressionService> mockStreamCompressionService,
            Stream sourceStream,
            Stream returnStream
        )
        {
            mockStreamCompressionService.Setup(m => m.DeCompressStream(
                It.Is<Stream>(s => s == sourceStream)
            ))
            .Returns(returnStream);
        }

        public static void VerifyDeCompressStream(
            this Mock<IStreamCompressionService> mockStreamCompressionService,
            Stream sourceStream
        )
        {
            mockStreamCompressionService.Verify(m => m.DeCompressStream(
                It.Is<Stream>(s => s == sourceStream)
            ));
        }
    }
}