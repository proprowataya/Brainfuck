using Brainfuck.Core.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Brainfuck.Core.Optimization
{
    public partial class Optimizer
    {
        private static IReadOnlyList<IOperation> OptimizeRoopStep(IReadOnlyList<IOperation> operations)
        {
            var list = new List<IOperation>();

            foreach (var op in operations)
            {
                if (op is IUnitOperation unit)
                {
                    IReadOnlyList<IOperation> optimized = null;
                    if (op is RoopUnitOperation roop)
                    {
                        optimized = TryOptimizeRoop(roop);
                        if (optimized != null)
                        {
                            list.AddRange(optimized);
                        }
                    }

                    if (optimized == null)
                    {
                        // Recursive optimize
                        IUnitOperation newUnit = unit.WithOperations(OptimizeRoopStep(unit.Operations).ToImmutableArray());
                        list.Add(newUnit);
                    }
                }
                else
                {
                    list.Add(op);
                }
            }

            return list;
        }

        private static IReadOnlyList<IOperation> TryOptimizeRoop(RoopUnitOperation roop)
        {
            if (roop.PtrChange != 0)
            {
                // If this roop changes ptr, we can't optimize this
                return null;
            }

            int origin = roop.Src.Offset;
            var deltas = new Dictionary<int, int>() { [origin] = 0 };

            void EnsureKey(int key)
            {
                if (!deltas.ContainsKey(key))
                    deltas[key] = 0;
            }

            foreach (var op in roop.Operations)
            {
                if (op is AddAssignOperation add)
                {
                    EnsureKey(add.Dest.Offset);
                    deltas[add.Dest.Offset] += add.Value;
                }
                else
                {
                    // We can't optimize this roop
                    return null;
                }
            }

            if (deltas[origin] != -1)
            {
                return null;
            }

            var optimized = new List<IOperation>();

            foreach (var p in deltas.Where(p => p.Key != origin))
            {
                // buffer[ptr + p.Key] += buffer[ptr] * p.Value
                optimized.Add(new MultAddAssignOperation(new MemoryLocation(p.Key), new MemoryLocation(origin), p.Value));
            }

            // buffer[ptr] = 0
            optimized.Add(new AssignOperation(new MemoryLocation(origin), 0));

            if (deltas.Where(p => p.Key < origin).Any())
            {
                // There are some accesses to buffer[ptr + x] where x < 0.
                // Such accesses sometimes cause out of range access.
                // So we have to make sure that buffer[ptr] != 0.

                // if (buffer[ptr] == 0) { goto `]`; }
                var iftrue = new IfTrueUnitOperation(optimized.ToImmutableArray(), 0, new MemoryLocation(origin));
                optimized.Clear();
                optimized.Add(iftrue);
            }

            return optimized;
        }
    }
}
