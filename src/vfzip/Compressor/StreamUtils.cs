using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vfzip.Compressor
{
    internal static class StreamUtils
    {
        public static async Task<int> ReadAsyncBlock(this Stream stream, Memory<byte> buffer, CancellationToken token)
        {
            var offset = 0;
            while (false == token.IsCancellationRequested && offset < buffer.Length)
            {
                var len = await stream.ReadAsync(buffer.Slice(offset), token);
                offset += len;
            }

            return buffer.Length;
        }
    }
}
