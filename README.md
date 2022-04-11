# vfzip
A zip tool for directories with lots of duplicate files.

Similar to tar, but deduplicates files before compression, acheiving an extremely high compression ratio.

```
.\vfzip.exe
Required command was not provided.

vfzip
  vfzip

Usage:
  vfzip [options] [command]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  compress <Source> <Target>    Compress a directory
  decompress <Source> <Target>  Decompress a directory
```

Example Usage:
```
# Compresion
> (Get-ChildItem C:\Code\Logship-Backend\bin\ -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
7254.15877342224
> .\vfzip.exe compress C:\Code\Logship-Backend\bin\ zipped.vfz
> (Get-ChildItem .\zipped.vfz).Length /  1MB
109.909594535828

# Decompression
> .\vfzip.exe decompress .\zipped.vfz output/
> (Get-ChildItem output/ -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
7254.15877342224
```
