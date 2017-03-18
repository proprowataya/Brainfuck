using System;
using System.Collections.Generic;
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
        #region Fields And Property

        private readonly Program program;
        private readonly ImmutableArray<Operation>.Builder operations;
        private readonly Dictionary<int, int> map;

        private ImmutableArray<Operation> Operations => program.Operations;

        #endregion

        public OptimizerImplement(Program program)
        {
            this.program = program;
            this.operations = ImmutableArray.CreateBuilder<Operation>();
            this.map = new Dictionary<int, int>();
        }

        public Program Optimize()
        {
            for (int i = 0; i < Operations.Length; i++)
            {
                Operation operation = Operations[i];
                if (operation.Opcode == Opcode.Unknown)
                {
                    continue;
                }

                if (operation.Opcode == Opcode.OpeningBracket)
                {
                    if (TryOptimizeSimpleRoop(i))
                    {
                        i = Operations[i].Value;
                        continue;
                    }
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
                else if (operation.Opcode == Opcode.ClosingBracket
                            && GetLast()?.Opcode == Opcode.ClosingBracket)
                {
                    // Successive closing brackets (']') can be reduced
                    map.Add(i, operations.Count - 1);
                    continue;
                }

                AddOperation(operation, i);
            }

            CorrectJumpAddresses();

            return new Program(program.Source, operations.ToImmutable());
        }

        private bool TryOptimizeSimpleRoop(int startIndex)
        {
            int endIndex = Operations[startIndex].Value;
            var deltas = new Dictionary<int, int>() { [0] = 0 };
            int offset = 0;

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                switch (Operations[i].Opcode)
                {
                    case Opcode.AddPtr:
                        offset += Operations[i].Value;
                        break;
                    case Opcode.AddValue:
                        if (!deltas.ContainsKey(offset))
                        {
                            deltas[offset] = 0;
                        }
                        deltas[offset] += Operations[i].Value;
                        break;
                    case Opcode.Unknown:
                        continue;
                    default:
                        // We can't optimize this roop
                        return false;
                }
            }

            if (offset != 0 || deltas[0] != -1)
            {
                return false;
            }

            // Generate optimized code

            if (deltas.Where(p => p.Key < 0).Any())
            {
                // There are some accesses to buffer[ptr + x] where x < 0.
                // Such accesses sometimes cause out of range access.
                // So we have to make sure that buffer[ptr] != 0.

                // if (buffer[ptr] == 0) { goto `]`; }
                AddCreatedOperation(new Operation(Opcode.BrZero, endIndex));
            }

            foreach (var p in deltas.Where(p => p.Key != 0))
            {
                // buffer[ptr + p.Key] += buffer[ptr] * p.Value
                AddCreatedOperation(new Operation(Opcode.MultAdd, p.Key, p.Value));
            }

            // buffer[ptr] = 0
            AddCreatedOperation(new Operation(Opcode.Assign, 0, 0));

            // We have to update address map manually
            map.Add(endIndex, operations.Count - 1);

            return true;
        }

        private void AddOperation(Operation operation, int index)
        {
            map.Add(index, operations.Count);   // Update address map
            operations.Add(operation);
        }

        private void AddCreatedOperation(Operation operation)
        {
            operations.Add(operation);
        }

        private void CorrectJumpAddresses()
        {
            for (int i = 0; i < operations.Count; i++)
            {
                switch (operations[i].Opcode)
                {
                    case Opcode.BrZero:
                    case Opcode.OpeningBracket:
                    case Opcode.ClosingBracket:
                        operations[i] = new Operation(operations[i].Opcode, map[operations[i].Value]);
                        break;
                }
            }
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
