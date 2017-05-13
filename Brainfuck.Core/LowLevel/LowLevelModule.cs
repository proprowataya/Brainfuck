using System.Collections.Immutable;

namespace Brainfuck.Core.LowLevel
{
    public class LowLevelModule
    {
        public ImmutableArray<LowLevelOperation> Operations { get; }
        public int NumRegisters { get; }

        public LowLevelModule(ImmutableArray<LowLevelOperation> operations, int numRegisters)
        {
            Operations = operations;
            NumRegisters = numRegisters;
        }
    }
}
