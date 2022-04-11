using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using vfzip.Compressor;

namespace vfzip.Commands
{
    internal static class DecompressCommand
    {
        public static Command CreateCommand()
        {
            var command = new Command("decompress", "Decompress a directory");
            command.AddArgument(new Argument<string>("Source", "The source vfz file"));
            command.AddArgument(new Argument<string>("Target", "The output directory."));
            command.Handler = CommandHandler.Create<string, string, CancellationToken>(RunAsync);
            return command;
        }

        private static async Task RunAsync(string source, string target, CancellationToken token)
        {
            var runner = new DirectoryDecompressor(Path.GetFullPath(source), Path.GetFullPath(target));
            await runner.ExecuteAsync(token);
        }
    }
}
