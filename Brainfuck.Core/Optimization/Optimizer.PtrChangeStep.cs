using Brainfuck.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Brainfuck.Core.Optimization
{
    public partial class Optimizer
    {
        private (IReadOnlyList<IOperation> operations, int ptrChange) OptimizePtrChangeStep(IReadOnlyList<IOperation> operations, int ptrChange)
        {
            int offset = 0, blockOriginOffset = 0;
            var list = new List<IOperation>();
            var delayed = new List<IOperation>();
            void EmitDelayedOpeations()
            {
                int nextPtrChange = offset - blockOriginOffset;
                if (delayed.Count > 0 || nextPtrChange != 0)
                {
                    var block = new BlockUnitOperation(delayed.ToImmutableArray(), nextPtrChange);
                    list.Add(block);
                    delayed.Clear();
                    blockOriginOffset = offset;
                }
            }

            for (int i = 0; i < operations.Count; i++)
            {
                Debug.Assert(!(operations[i] is AddPtrOperation));

                IOperation op = operations[i].WithAdd(-offset);

                var accessLocations = AccessLocations(op).ToArray();
                int minLocationDiff =
                    accessLocations.Length == 0 ? 0 : accessLocations.Select(l => Math.Abs(l.Offset)).Min();

                if (minLocationDiff > PtrDiffThreshold || op is RoopUnitOperation || op is IfTrueUnitOperation)
                {
                    int adjustOffset;
                    if (op is RoopUnitOperation roop)
                    {
                        adjustOffset = roop.Src.Offset;
                    }
                    else if (op is IfTrueUnitOperation iftrue)
                    {
                        adjustOffset = iftrue.Src.Offset;
                    }
                    else
                    {
                        adjustOffset = accessLocations.First().Offset;   // TODO
                    }

                    offset += adjustOffset;
                    EmitDelayedOpeations(); // Emit delayed operations
                    op = op.WithAdd(-adjustOffset);
                }

                if (op is IUnitOperation unit)
                {
                    // Optimize recursive
                    var optimized = OptimizePtrChangeStep(unit.Operations, unit.PtrChange);
                    delayed.Add(unit.WithOperations(optimized.operations.ToImmutableArray())
                                    .WithPtrChange(optimized.ptrChange));

                }
                else
                {
                    delayed.Add(op);
                }
            }

            EmitDelayedOpeations();
            return (list, ptrChange - offset);
        }

        private static IEnumerable<MemoryLocation> AccessLocations(IOperation operation)
        {
            if (operation is IReadOperation read)
            {
                yield return read.Src;
            }

            if (operation is IWriteOperation write)
            {
                yield return write.Dest;
            }
        }

        // Currently, this value is constant and don't depend on favor.
        private int PtrDiffThreshold => 2;
    }
}
