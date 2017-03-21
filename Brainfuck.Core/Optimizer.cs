using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Brainfuck.Core
{
    public static class Optimizer
    {
        public static Module Optimize(this Module module) => module;

#if false
        public static Module Optimize(this Module module)
        {
            var operations = module.Operations;
            operations = OptimizeRoopStep(operations);
            operations = OptimizeReduceStep(operations, 0, updateOffsetAtLast: true);
            return new Module(module.Source, operations);
        }

        private static ImmutableArray<IOperation> OptimizeRoopStep(IReadOnlyList<IOperation> operations)
        {
            var list = new List<IOperation>();

            foreach (var op in operations)
            {
                if (op is Roop roop)
                {
                    var optimized = TryOptimizeRoop(roop);
                    if (optimized != null)
                    {
                        list.AddRange(optimized);
                    }
                    else
                    {
                        list.Add(new Roop(OptimizeRoopStep(roop.Operations)));
                    }
                }
                else
                {
                    list.Add(op);
                }
            }

            return ImmutableArray.CreateRange(list);
        }

        private static IReadOnlyList<IOperation> TryOptimizeRoop(Roop roop)
        {
            var deltas = new Dictionary<int, int>() { [0] = 0 };
            int offset = 0;

            void EnsureKey(int key)
            {
                if (!deltas.ContainsKey(key))
                    deltas[key] = 0;
            }

            foreach (var op in roop.Operations)
            {
                if (op is AddPtr addptr)
                {
                    offset += addptr.Value;
                }
                else if (op is AddValue addvalue)
                {
                    int dest = offset + addvalue.Dest.Offset;
                    EnsureKey(dest);
                    deltas[dest] += addvalue.Value;
                }
                else
                {
                    // We can't optimize this roop
                    return null;
                }
            }

            if (offset != 0 || deltas[0] != -1)
            {
                return null;
            }

            var optimized = new List<IOperation>();

            foreach (var p in deltas.Where(p => p.Key != 0))
            {
                // buffer[ptr + p.Key] += buffer[ptr] * p.Value
                optimized.Add(new MultAdd(new MemoryLocation(p.Key), MemoryLocation.Zero, p.Value));
            }

            // buffer[ptr] = 0
            optimized.Add(new Assign(MemoryLocation.Zero, 0));

            if (deltas.Where(p => p.Key < 0).Any())
            {
                // There are some accesses to buffer[ptr + x] where x < 0.
                // Such accesses sometimes cause out of range access.
                // So we have to make sure that buffer[ptr] != 0.

                // if (buffer[ptr] == 0) { goto `]`; }
                IfTrue iftrue = new IfTrue(MemoryLocation.Zero, ImmutableArray.CreateRange(optimized));
                optimized.Clear();
                optimized.Add(iftrue);
            }

            return optimized;
        }

        private static ImmutableArray<IOperation> OptimizeReduceStep(IReadOnlyList<IOperation> operations, int offset, bool updateOffsetAtLast)
        {
            var list = new List<IOperation>();

            int i = 0;
            while (i < operations.Count)
            {
                IOperation op = operations[i].WithAddLocation(offset);

                if (op is Roop roop)
                {
                    list.Add(new AddPtr(offset));
                    offset = 0;
                    list.Add(new Roop(OptimizeReduceStep(roop.Operations, offset, updateOffsetAtLast: true)));
                    i++;
                }
                else
                {
                    var optimized = StartReduce(operations, ref offset, ref i);
                    list.AddRange(optimized);
                }
            }

            if (updateOffsetAtLast)
            {
                list.Add(new AddPtr(offset));
            }

            // Eliminate no effect operations
            IEnumerable<IOperation> eliminated = list.Where(op => !HasNoEffect(op));
            return ImmutableArray.CreateRange(eliminated);
        }

        private static IReadOnlyList<IOperation> StartReduce(IReadOnlyList<IOperation> operations, ref int offset, ref int i)
        {
            var list = new List<IOperation>();
            //var depends = new HashSet<MemoryLocation>();
            var lastWrites = new Dictionary<MemoryLocation, IWriteOperation>();

            for (; i < operations.Count && !(operations[i] is Roop); i++)
            {
                IOperation op = operations[i].WithAddLocation(offset);
                IOperation add = null;
                List<IOperation> totallyAdded = new List<IOperation>();

                if (op is IfTrue iftrue)
                {
                    // Assume that IfTrue doesn't change ptr as a result
                    var optimized = new IfTrue(iftrue.Condition, OptimizeReduceStep(iftrue.Operations, offset, updateOffsetAtLast: false));
                    add = optimized;
                    totallyAdded.AddRange(optimized.Operations);
                }
                else if (op is AddPtr addptr)
                {
                    offset += addptr.Value;
                }
                else if (op is IWriteOperation write)
                {
                    // Find a candidate to be reduced with 'op'
                    lastWrites.TryGetValue(write.Dest, out var lastReducable);

                    // If the candidate was found and can be reducable
                    if (lastReducable != null && TryReduce(lastReducable, write) is IOperation reduced)
                    {
                        list.Remove(lastReducable);
                        add = reduced;
                    }
                    else
                    {
                        add = op;
                    }
                }
                else
                {
                    add = op;
                }

                // Update lastWrites
                totallyAdded.Add(add);
                var writes = totallyAdded.OfType<IWriteOperation>();
                var reads = totallyAdded.OfType<IReadOperation>();

                foreach (var iwrite in writes)
                {
                    lastWrites[iwrite.Dest] = iwrite;
                }

                foreach (var iread in reads)
                {
                    lastWrites.Remove(iread.Src);
                }

                // Update list
                if (add != null)
                {
                    list.Add(add);
                }
            }

            return list;
        }

        private static IOperation TryReduce(dynamic a, dynamic b) => _TryReduce(a, b);

        #region Reduce methods

        private static IOperation _TryReduce(IOperation a, IOperation b)
        {
            // Default implementation
            return null;
        }

        private static IOperation _TryReduce(AddPtr a, AddPtr b)
        {
            return new AddPtr(a.Value + b.Value);
        }

        private static IOperation _TryReduce(AddValue a, AddValue b)
        {
            if (a.Dest == b.Dest)
                return new AddValue(a.Dest, a.Value + b.Value);
            else
                return null;
        }

        private static IOperation _TryReduce(Assign a, AddValue b)
        {
            if (a.Dest == b.Dest)
                return new Assign(a.Dest, a.Value + b.Value);
            else
                return null;
        }

        private static IOperation _TryReduce(IWriteOperation a, Assign b)
        {
            if (a is Read || a is DummyWriteOp)
                return null;
            if (a.Dest == b.Dest)
                return b;
            else
                return null;
        }

        #endregion

        private static bool HasNoEffect(IOperation operation)
        {
            switch (operation)
            {
                case AddPtr op:
                    return op.Value == 0;
                case AddValue op:
                    return op.Value == 0;
                case MultAdd op:
                    return op.Value == 0;
                case DummyWriteOp op:
                    return true;
                default:
                    return false;
            }
        }
#endif
    }
}
