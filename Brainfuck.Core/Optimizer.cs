using Brainfuck.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Brainfuck.Core
{
    public static class Optimizer
    {
        public static Module Optimize(this Module module)
        {
            IReadOnlyList<IOperation> operations = module.Root.Operations;
            operations = OptimizeRoopStep(operations);
            operations = OptimizeReduceStep(operations);
            return new Module(module.Source, new BlockUnitOperation(operations.ToImmutableArray(), module.Root.PtrChange));
        }

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

        private static IReadOnlyList<IOperation> OptimizeReduceStep(IReadOnlyList<IOperation> operations)
        {
            var list = new List<(int id, IOperation operation)>();
            var lastWrites = new Dictionary<MemoryLocation, (int id, IOperation operation)>();
            int nextID = 0;
            void Add(IOperation operation) => list.Add((nextID++, operation));

            for (int i = 0; i < operations.Count; i++)
            {
                IOperation op = operations[i];
                Debug.Assert(!(op is AddPtrOperation));

                if (op is IUnitOperation unit)
                {
                    // Recursive optimize
                    var optimized = OptimizeReduceStep(unit.Operations);
                    Add(unit.WithOperations(optimized.ToImmutableArray()));

                    // Prevent further reduce
                    lastWrites.Clear();
                }
                else if (op is IWriteOperation write)
                {
                    // Find a candidate to be reduced with 'op'
                    lastWrites.TryGetValue(write.Dest, out var lastReducable);

                    // If the candidate was found and can be reducable
                    if (lastReducable.operation != null)
                    {
                        var reduced = TryReduce(lastReducable.operation, write);
                        if (reduced != null)
                        {
                            list.RemoveAll(t => t.id == lastReducable.id);
                            Add(reduced);
                        }
                        else
                        {
                            Add(op);
                        }
                    }
                    else
                    {
                        Add(op);
                    }
                }
                else
                {
                    Add(op);
                }

                // Update lastWrites
                if (list.Last().operation is IWriteOperation iwrite)
                {
                    lastWrites[iwrite.Dest] = list.Last();
                }

                // If this operation reads some memory locations,
                // we must not to reduce currently existing opeations which write to the locations.
                {
                    if (list.Last().operation is IReadOperation iread)
                    {
                        lastWrites.Remove(iread.Src);
                    }

                    if (list.Last().operation is IUnitOperation iunit)
                    {
                        foreach (var item in iunit.Operations.OfType<IReadOperation>())
                        {
                            lastWrites.Remove(item.Src);
                        }
                    }
                }
            }

            return list.Select(t => t.operation).Where(op => !HasNoEffect(op)).ToArray();
        }

        private static IOperation TryReduce(IOperation a, IOperation b)
        {
            if (a is IWriteOperation wa && b is IWriteOperation wb && wa.Dest == wb.Dest)
            {
                MemoryLocation dest = wa.Dest;

                if (a is AddAssignOperation addA && b is AddAssignOperation addB)
                {
                    return new AddAssignOperation(dest, addA.Value + addB.Value);
                }
                else if (a is AssignOperation assignA && b is AddAssignOperation add)
                {
                    return new AssignOperation(dest, assignA.Value + add.Value);
                }
                else if (a is IWriteOperation && !(a is ReadOperation) && b is AssignOperation assignB)
                {
                    return assignB;
                }
            }

            return null;
        }

        private static bool HasNoEffect(IOperation operation)
        {
            switch (operation)
            {
                case AddAssignOperation op:
                    return op.Value == 0;
                case MultAddAssignOperation op:
                    return op.Value == 0;
                default:
                    return false;
            }
        }
    }
}
