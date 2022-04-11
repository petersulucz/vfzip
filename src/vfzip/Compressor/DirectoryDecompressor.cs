using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vfzip.Compressor
{
    internal class DirectoryDecompressor
    {
        private readonly string sourcePath;
        private readonly string targetDirectory;
        private readonly string hashedFilesDirectory;
        
        public DirectoryDecompressor(
            string sourcePath,
            string targetDirectory)
        {
            this.sourcePath = sourcePath;
            this.targetDirectory = targetDirectory;
            this.hashedFilesDirectory = Path.Combine(this.targetDirectory, ".vfz");
        }

        public async Task ExecuteAsync(CancellationToken token)
        {
            Directory.CreateDirectory(this.hashedFilesDirectory);

            using (var inputStream = File.OpenRead(this.sourcePath))
            {
                using (var decompressorStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    while (false == token.IsCancellationRequested)
                    {
                        var messageType = decompressorStream.ReadByte();
                        switch(messageType)
                        {
                            case -1:
                                // End of stream.
                                return;
                            case 1:
                                await this.DeflateFileFromStream(decompressorStream, token);
                                break;
                            case 2:
                                await this.ProcessFileTree(decompressorStream, token);
                                break;
                            default:
                                throw new NotImplementedException("File is corrupted, or not supported by this version");
                        }
                    }
                }
            }
        }

        private async Task DeflateFileFromStream(Stream inputStream, CancellationToken token)
        {
            var hash = new byte[20];
            await inputStream.ReadAsyncBlock(hash, token);
            var lengthBytes = new byte[8];
            await inputStream.ReadAsyncBlock(lengthBytes, token);
            var length = BitConverter.ToInt64(lengthBytes);

            var buffer = new byte[8192];
            var outputPath = Path.Combine(this.hashedFilesDirectory, BitConverter.ToString(hash));
            using (var writer = File.OpenWrite(outputPath))
            {
                while (length > 0 && false == token.IsCancellationRequested)
                {
                    var readBytes = await inputStream.ReadAsync(buffer, 0, (int)Math.Min((long)buffer.Length, length), token);
                    await writer.WriteAsync(buffer, 0, readBytes, token);
                    length -= readBytes;
                }
            }
        }

        private async Task ProcessFileTree(Stream inputStream, CancellationToken token)
        {
            var lengthBytes = new byte[4];
            await inputStream.ReadAsyncBlock(lengthBytes, token);

            var fileNameBytes = new byte[BitConverter.ToInt32(lengthBytes)];
            await inputStream.ReadAsyncBlock(fileNameBytes, token);

            var hashBytes = new byte[20];
            await inputStream.ReadAsyncBlock(hashBytes, token);

            var fullPath = Path.Combine(this.targetDirectory, Encoding.UTF8.GetString(fileNameBytes));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            var linkPath = Path.Combine(this.hashedFilesDirectory, BitConverter.ToString(hashBytes));
            Console.WriteLine("Creating link: {0} -> {1}", fullPath, linkPath);

            File.Copy(linkPath, fullPath, true);
        }
    }
}
