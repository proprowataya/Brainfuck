using System;

namespace Brainfuck.Core
{
    public class Setting
    {
        public int BufferSize { get; }
        public Type ElementType { get; }
        public bool UseDynamicBuffer { get; }
        public bool UnsafeCode { get; }

        public Setting(int bufferSize, Type elementType, bool useDynamicBuffer, bool unsafeCode)
        {
            BufferSize = bufferSize;
            ElementType = elementType;
            UseDynamicBuffer = useDynamicBuffer;
            UnsafeCode = unsafeCode;
        }

        public static readonly Setting Default = new Setting(
            Defaults.BufferSize, Defaults.ElementType, Defaults.UseDynamicBuffer, Defaults.UnsafeCode);

        public Setting WithBufferSize(int bufferSize)
            => new Setting(bufferSize, this.ElementType, this.UseDynamicBuffer, this.UnsafeCode);
        public Setting WithElementType(Type elementType)
            => new Setting(this.BufferSize, elementType, this.UseDynamicBuffer, this.UnsafeCode);
        public Setting WithUseDynamicBuffer(bool useDynamicBuffer)
            => new Setting(this.BufferSize, this.ElementType, useDynamicBuffer, this.UnsafeCode);
        public Setting WithUnsafeCode(bool unsafeCode)
            => new Setting(this.BufferSize, this.ElementType, this.UseDynamicBuffer, unsafeCode);
    }
}
