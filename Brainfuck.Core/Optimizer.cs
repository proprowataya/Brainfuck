using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Brainfuck.Core
{
    public static class Optimizer
    {
        public static Program Optimize(Program program)
        {
            var operations = ImmutableArray.CreateBuilder<Operation>();
            Operation? GetLast() => operations.Count > 0 ? operations.Last() : (Operation?)null;
            Operation RemoveLast()
            {
                Operation last = operations.Last();
                operations.RemoveAt(operations.Count - 1);
                return last;
            }

            var map = new Dictionary<int, int>();

            for (int i = 0; i < program.Operations.Length; i++)
            {
                Operation operation = program.Operations[i];
                if (operation.Opcode == Opcode.Unknown)
                {
                    continue;
                }

                // If possible, We reduce operation
                if (IsReducible(operation.Opcode) && GetLast()?.Opcode == operation.Opcode)
                {
                    Operation last = RemoveLast();
                    operation = new Operation(operation.Opcode, last.Value + operation.Value);
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
