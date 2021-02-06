using System;

namespace Brainfuck.Core.Interpretation
{
    public delegate void OnStepStartEventHandler(OnStepStartEventArgs args);
    public sealed record OnStepStartEventArgs(Array Buffer, int BufferPointer, int ProgramPointer, long Step);
}
