using System;

namespace Brainfuck.Core
{
    public class Setting
    {
        public int BufferSize { get; }
        public Type ElementType { get; }
        public bool UseDynamicBuffer { get; }

        public Setting(int bufferSize, Type elementType, bool useDynamicBuffer)
        {
            BufferSize = bufferSize;
            ElementType = elementType;
            UseDynamicBuffer = useDynamicBuffer;
        }

        public static readonly Setting Default = new Setting(Defaults.BufferSize, Defaults.ElementType, Defaults.UseDynamicBuffer);

        public Setting WithBufferSize(int bufferSize) => new Setting(bufferSize, this.ElementType, this.UseDynamicBuffer);
        public Setting WithElementType(Type elementType) => new Setting(this.BufferSize, elementType, this.UseDynamicBuffer);
        public Setting WithUseDynamicBuffer(bool useDynamicBuffer) => new Setting(this.BufferSize, this.ElementType, useDynamicBuffer);
    }
}
