using System;

namespace Brainfuck.Core
{
    public class Setting
    {
        public int BufferSize { get; }
        public Type ElementType { get; }
        public bool UseDynamicBuffer { get; }
        public bool UnsafeCode { get; }
        public Favor Favor { get; }

        public Setting(int bufferSize, Type elementType, bool useDynamicBuffer, bool unsafeCode, Favor favor)
        {
            BufferSize = bufferSize;
            ElementType = elementType;
            UseDynamicBuffer = useDynamicBuffer;
            UnsafeCode = unsafeCode;
            Favor = favor;
        }

        public static readonly Setting Default = new Setting(
            Defaults.BufferSize, Defaults.ElementType, Defaults.UseDynamicBuffer, Defaults.UnsafeCode, Favor.Default);

        public Setting WithBufferSize(int bufferSize)
            => new Setting(bufferSize, this.ElementType, this.UseDynamicBuffer, this.UnsafeCode, this.Favor);
        public Setting WithElementType(Type elementType)
            => new Setting(this.BufferSize, elementType, this.UseDynamicBuffer, this.UnsafeCode, this.Favor);
        public Setting WithUseDynamicBuffer(bool useDynamicBuffer)
            => new Setting(this.BufferSize, this.ElementType, useDynamicBuffer, this.UnsafeCode, this.Favor);
        public Setting WithUnsafeCode(bool unsafeCode)
            => new Setting(this.BufferSize, this.ElementType, this.UseDynamicBuffer, unsafeCode, this.Favor);
        public Setting WithFavor(Favor favor)
            => new Setting(this.BufferSize, this.ElementType, this.UseDynamicBuffer, this.UnsafeCode, favor);
    }

    public enum Favor
    {
        Default, ILSafe, ILUnsafe, Interpreter,
    }
}
