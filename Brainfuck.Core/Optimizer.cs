﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Brainfuck.Core
{
    public static class Optimizer
    {
        public static Program Optimize(this Program program)
        {
            return new Program(program.Source, OptimizeReduceStep(program.Operations));
        }

        private static ImmutableArray<IOperation> OptimizeReduceStep(IReadOnlyList<IOperation> operations, int offset = 0)
        {
            var optimized = new List<IOperation>();
            var sublist = new List<IOperation>();

            void EmitAndReset()
            {
                optimized.AddRange(sublist);
                optimized.Add(new AddPtr(offset));
                sublist.Clear();
                offset = 0;
            }

            for (int i = 0; i < operations.Count; i++)
            {
                IOperation op = operations[i].WithAddLocation(offset);

                if (op is Roop roop)
                {
                    // Emit delayed code and clear state
                    EmitAndReset();

                    // Optimize inner code
                    optimized.Add(new Roop(OptimizeReduceStep(roop.Operations)));
                }
                else if (op is IfTrue iftrue)
                {
                    // Assume that IfTrue doesn't change ptr as a result
                    optimized.Add(new IfTrue(iftrue.Condition, OptimizeReduceStep(iftrue.Operations, offset)));
                }
                else if (op is AddPtr addptr)
                {
                    offset += addptr.Value;
                }
                else if (op is Put put)
                {
                    sublist.Add(put);

                    // In order to suppress elimination of adds before put operation,
                    // We derive DummyWriteOp
                    sublist.Add(new DummyWriteOp(new MemoryLocation(offset)));
                }
                else if (op is IWriteOperation write)
                {
                    // Find a candidate to be reduced with 'op'
                    IWriteOperation lastReducable = sublist.OfType<IWriteOperation>()
                                                           .LastOrDefault(x => x.Dest == write.Dest);

                    // If the candidate was found and can be reducable
                    if (lastReducable != null && TryReduce(lastReducable, write) is IOperation reduced)
                    {
                        sublist.Remove(lastReducable);
                        sublist.Add(reduced);
                    }
                    else
                    {
                        sublist.Add(op);
                    }
                }
                else
                {
                    sublist.Add(op);
                }
            }

            EmitAndReset();

            // Eliminate no effect operations
            IEnumerable<IOperation> eliminated = optimized.Where(op => !HasNoEffect(op));
            return ImmutableArray.CreateRange(eliminated);
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
    }
}
