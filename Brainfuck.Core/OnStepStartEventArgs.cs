using System.Collections.Generic;
using System.Collections;
using Brainfuck.Core.LowLevel;
using System;

namespace Brainfuck.Core
{
    public delegate void OnStepStartEventHandler(OnStepStartEventArgs args);

    public sealed class OnStepStartEventArgs
    {
        public Array Buffer { get; }
        public int BufferPointer { get; }
        public int ProgramPointer { get; }
        public long Step { get; }

        public OnStepStartEventArgs(Array buffer, int bufferPointer, int programPointer, long step)
        {
            Buffer = buffer;
            BufferPointer = bufferPointer;
            ProgramPointer = programPointer;
            Step = step;
        }
    }
}
