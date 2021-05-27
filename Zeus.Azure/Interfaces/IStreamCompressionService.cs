using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure
{
    public interface IStreamCompressionService
    {
        Stream CompressStream(Stream sourceStream, string entryName);
        Stream DeCompressStream(Stream compressedStream);
    }
}