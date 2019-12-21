using System;

namespace Brainfuck.Core
{
    internal static class Defaults
    {
        internal const int BufferSize = 1 << 20;
        internal static readonly Type ElementType = typeof(Int32);
        internal const bool UseDynamicBuffer = false;
        internal const bool UnsafeCode = false;
    }
}
