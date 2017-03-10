﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Brainfuck.Core
{
    public static class Optimizer
    {
        public static Program Optimize(this Program program) => new OptimizerImplement(program).Optimize();
    }

    internal struct OptimizerImplement
    {
        #region Fields

        private readonly Program program;
        private readonly ImmutableArray<Operation>.Builder operations;
        private readonly Dictionary<int, int> map;

        #endregion

        public OptimizerImplement(Program program)
        {
            this.program = program;
            this.operations = ImmutableArray.CreateBuilder<Operation>();
            this.map = new Dictionary<int, int>();
        }

        public Program Optimize()
        {
            for (int i = 0; i < program.Operations.Length; i++)
            {
                Operation operation = program.Operations[i];
                if (operation.Opcode == Opcode.Unknown)
                {
                    continue;
                }

                if (IsReducible(operation.Opcode))
                {
                    // If possible, we reduce operation
                    if (GetLast()?.Opcode == operation.Opcode)
                    {
                        Operation last = RemoveLast();
                        operation = new Operation(operation.Opcode, last.Value + operation.Value);
                    }

                    if (operation.Value == 0)
                    {
                        // This operation has no effect, so we ignore it
                        continue;
                    }
                }

                // Update address map
                map.Add(i, operations.Count);

                // Add operation
                operations.Add(operation);
            }

            for (int i = 0; i < operations.Count; i++)
            {
                switch (operations[i].Opcode)
                {
                    case Opcode.OpeningBracket:
                    case Opcode.ClosingBracket:
                        operations[i] = new Operation(operations[i].Opcode, map[operations[i].Value]);
                        break;
                }
            }

            return new Program(program.Source, operations.ToImmutable());
        }

        private Operation? GetLast() => operations.Count > 0 ? operations.Last() : (Operation?)null;
        private Operation RemoveLast()
        {
            Operation last = operations.Last();
            operations.RemoveAt(operations.Count - 1);
            return last;
        }

        private static bool IsReducible(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.AddPtr:
                case Opcode.AddValue:
                    return true;
                default:
                    return false;
            }
        }
    }
}
