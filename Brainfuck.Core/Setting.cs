using System;

namespace Brainfuck.Core
{
    public record Setting
    {
        public int BufferSize { get; init; }
        public Type ElementType { get; init; }
        public bool UseDynamicBuffer { get; init; }
        public bool UnsafeCode { get; init; }
        public Favor Favor { get; init; }

        public static readonly Setting Default = new Setting
        {
            BufferSize = Defaults.BufferSize,
            ElementType = Defaults.ElementType,
            UseDynamicBuffer = Defaults.UseDynamicBuffer,
            UnsafeCode = Defaults.UnsafeCode,
            Favor = Favor.Default,
        };
    }

    public enum Favor
    {
        Default, ILSafe, ILUnsafe, Interpreter,
    }
}
