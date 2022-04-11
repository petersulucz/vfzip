using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using vfzip.Compressor;

namespace vfzip.Commands
{
    internal static class CompressCommand
    {
        public static Command CreateCommand()
        {
            var command = new Command("compress", "Compress a directory");
            command.AddArgument(new Argument<string>("Source", "The directory to compress"));
            command.AddArgument(new Argument<string>("Target", "The output file"));
            command.Handler = CommandHandler.Create<string, string, CancellationToken>(RunAsync);
            return command;
        }

        private static async Task RunAsync(string source, string target, CancellationToken token)
        {
            var runner = new DirectoryCompressor(Path.GetFullPath(source), Path.GetFullPath(target));
            await runner.ExecuteAsync(token);
        }
    }
}
