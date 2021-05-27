using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeus.Azure.Storage
{
    public class BlobDownloadException : Exception
    {
        public BlobDownloadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}