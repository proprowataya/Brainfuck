using System;
using System.Collections.Generic;
using System.Text;

namespace Brainfuck.Core.LowLevel
{
    public static partial class LowLevelConverter
    {
        private static (IReadOnlyList<LowLevelOperation> operations, int numRegisters)
            OptimizeRegisterAlloc(IReadOnlyList<LowLevelOperation> operations)
        {
            return (operations, 0);
        }
    }
}
