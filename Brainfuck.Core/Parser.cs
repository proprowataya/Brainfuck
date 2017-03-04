using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Brainfuck.Core
{
    public static class Parser
    {
        public static Program Parse(string source)
        {
            var operations = ImmutableArray.CreateBuilder<Operation>(source.Length);
            var stack = new Stack<int>();
            var brackets = new List<int>();
            var jumpAddress = new Dictionary<int, int>();

            for (int i = 0; i < source.Length; i++)
            {
                switch (source[i])
                {
                    case '>':
                        operations.Add(new Operation(Opcode.AddPtr, 1));
                        break;
                    case '<':
                        operations.Add(new Operation(Opcode.AddPtr, -1));
                        break;
                    case '+':
                        operations.Add(new Operation(Opcode.AddValue, 1));
                        break;
                    case '-':
                        operations.Add(new Operation(Opcode.AddValue, -1));
                        break;
                    case '.':
                        operations.Add(new Operation(Opcode.Put));
                        break;
                    case ',':
                        operations.Add(new Operation(Opcode.Read));
                        break;
                    case '[':
                        {
                            stack.Push(i);
                            brackets.Add(i);
                            operations.Add(new Operation(Opcode.Unknown));
                            break;
                        }
                    case ']':
                        {
                            int startAddress = stack.Pop();
                            jumpAddress.Add(startAddress, i);
                            operations.Add(new Operation(Opcode.ClosingBracket, startAddress));
                            break;
                        }
                    default:
                        ManageUnknownChar(source[i]);
                        break;
                }
            }

            foreach (var index in brackets)
            {
                operations[index] = new Operation(Opcode.OpeningBracket, jumpAddress[index]);
            }

            Debug.Assert(operations.Count == source.Length);
            return new Program(source, operations.MoveToImmutable());
        }

        private static void ManageUnknownChar(char value) => Console.WriteLine($"Warning : Unknown char '{value}'");
    }
}
