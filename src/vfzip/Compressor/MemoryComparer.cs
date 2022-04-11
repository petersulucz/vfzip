using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace vfzip.Compressor
{
    internal class MemoryComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public static IEqualityComparer<ReadOnlyMemory<byte>> Instance = new MemoryComparer();

        private MemoryComparer()
        {
        }

        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            return x.Span.SequenceEqual(y.Span);
        }

        public int GetHashCode([DisallowNull] ReadOnlyMemory<byte> obj)
        {
            return new BigInteger(obj.Span).GetHashCode();
        }
    }
}
