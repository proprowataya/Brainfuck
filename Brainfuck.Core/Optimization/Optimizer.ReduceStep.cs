using Brainfuck.Core.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Brainfuck.Core.Optimization
{
    public partial class Optimizer
    {
        private static IReadOnlyList<IOperation> OptimizeReduceStep(IReadOnlyList<IOperation> operations)
        {
            // Optimized operations.
            // ID is used to identify which elenent we remove.
            var list = new List<(int id, IOperation operation)>();

            // Candidates to be reduced.
            // candidates[location] is an operation which writes to the location,
            // and it can be reduced by further operation.
            var candidates = new Dictionary<MemoryLocation, (int id, IOperation operation)>();

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
                    candidates.Clear();
                }
                else if (op is IWriteOperation write)
                {
                    // Find a candidate to be reduced with 'op'
                    candidates.TryGetValue(write.Dest, out var candidate);

                    // If the candidate was found and can be reducable
                    if (candidate.operation != null)
                    {
                        var reduced = TryReduce(candidate.operation, write);
                        if (reduced != null)
                        {
                            list.RemoveAll(t => t.id == candidate.id);
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

                // Update candidates
                if (list.Last().operation is IWriteOperation iwrite)
                {
                    candidates[iwrite.Dest] = list.Last();
                }

                // If this operation reads some memory locations,
                // we must not to reduce currently existing opeations which write to the locations.
                {
                    if (list.Last().operation is IReadOperation iread)
                    {
                        candidates.Remove(iread.Src);
                    }

                    if (list.Last().operation is IUnitOperation iunit)
                    {
                        foreach (var item in iunit.Operations.OfType<IReadOperation>())
                        {
                            candidates.Remove(item.Src);
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
