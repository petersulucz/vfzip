using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vfzip.Compressor
{
    internal class DirectoryCompressor
    {
        private readonly string sourceDirectory;
        private readonly string targetPath;
        private readonly int threads;

        private readonly SemaphoreSlim gate = new SemaphoreSlim(0);

        private readonly ConcurrentQueue<KeyValuePair<ReadOnlyMemory<byte>, FileInfo>> outputQueue
            = new ConcurrentQueue<KeyValuePair<ReadOnlyMemory<byte>, FileInfo>>();
        private readonly HashSet<ReadOnlyMemory<byte>> writtenHashes = new HashSet<ReadOnlyMemory<byte>>(MemoryComparer.Instance);
        private readonly SortedDictionary<string, ReadOnlyMemory<byte>> fileTree = new SortedDictionary<string, ReadOnlyMemory<byte>>();

        public DirectoryCompressor(
            string sourceDirectory,
            string targetPath,
            int threads = 8)
        {
            this.sourceDirectory = sourceDirectory;
            this.targetPath = targetPath;
            this.threads = threads;
        }

        public async Task ExecuteAsync(CancellationToken token)
        {
            await Task.WhenAll(
                Task.Run(() => this.ReadWorker(token)),
                Task.Run(() => this.WriteWorker(token)));
        }

        private async Task ReadWorker(CancellationToken token)
        {
            var semaphore = new SemaphoreSlim(0);
            var queue = new ConcurrentQueue<FileInfo>();
            var threads = Enumerable.Range(0, this.threads)
                .Select(i => Task.Run(() => this.ReadProcessor(queue, semaphore, token))).ToList();

            foreach (var file in Directory.EnumerateFiles(this.sourceDirectory, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true
            }))
            {
                queue.Enqueue(new FileInfo(file));
                semaphore.Release();
            }

            semaphore.Release(this.threads);
            await Task.WhenAll(threads);
            this.gate.Release();
        }

        private async Task ReadProcessor(ConcurrentQueue<FileInfo> files, SemaphoreSlim semaphore, CancellationToken token)
        {
            while (false == token.IsCancellationRequested)
            {
                await semaphore.WaitAsync(token);
                if (false == files.TryDequeue(out var file))
                {
                    break;
                }

                using (var hasher = SHA1.Create())
                using (var fileStream = file.OpenRead())
                {
                    var hash = await hasher.ComputeHashAsync(fileStream);

                    this.outputQueue.Enqueue(new KeyValuePair<ReadOnlyMemory<byte>, FileInfo>(hash, file));
                    this.gate.Release();
                }
            }
        }

        private async Task WriteWorker(CancellationToken token)
        {
            using (var outputFile = File.OpenWrite(this.targetPath))
            {
                using (var outputStream = new DeflateStream(outputFile, CompressionLevel.Optimal, true))
                {
                    while (false == token.IsCancellationRequested)
                    {
                        await this.gate.WaitAsync(token);
                        if (false == this.outputQueue.TryDequeue(out var output))
                        {
                            Console.WriteLine("Writing header");

                            foreach (var file in this.fileTree)
                            {
                                outputStream.WriteByte(2);
                                var path = Encoding.UTF8.GetBytes(file.Key);
                                await outputStream.WriteAsync(BitConverter.GetBytes(path.Length), token);
                                await outputStream.WriteAsync(path, token);
                                await outputStream.WriteAsync(file.Value, token);
                            }

                            return;
                        }

                        var relativePath = Path.GetRelativePath(this.sourceDirectory, output.Value.FullName);
                        if (Path.DirectorySeparatorChar != '/')
                        {
                            relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                        }
                        this.fileTree.Add(relativePath, output.Key);

                        var hexString = BitConverter.ToString(output.Key.ToArray());
                        if (this.writtenHashes.Contains(output.Key))
                        {
                            Console.WriteLine("Already written: {0} -> {1}", relativePath, hexString);
                            continue;
                        }

                        Console.WriteLine("Writing: {0} -> {1}", relativePath, hexString);

                        outputStream.WriteByte(1);
                        await outputStream.WriteAsync(output.Key, token);
                        var length = BitConverter.GetBytes((long)output.Value.Length);
                        await outputStream.WriteAsync(length, token);

                        using (var sourceFile = output.Value.OpenRead())
                        {
                            await sourceFile.CopyToAsync(outputStream);
                        }

                        this.writtenHashes.Add(output.Key);
                    }
                }
            }
        }
    }
}
