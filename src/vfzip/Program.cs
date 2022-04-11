using System;
using System.CommandLine;
using vfzip.Commands;
using vfzip.Compressor;

var rootCommand = new RootCommand("vfzip");
rootCommand.AddCommand(CompressCommand.CreateCommand());
rootCommand.AddCommand(DecompressCommand.CreateCommand());
rootCommand.Invoke(args);
