using Brainfuck.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Brainfuck.Core
{
    public static class Parser
    {
        public static Module Parse(string source)
        {
            int i = 0;
            var t = ParseUnit(source, 0, ref i);
            return new Module(source, new BlockUnitOperation(t.operations, t.ptrChange));
        }

        private static (ImmutableArray<IOperation> operations, int ptrChange) ParseUnit(string source, int firstOffset, ref int i)
        {
            int offset = firstOffset;
            var builder = ImmutableArray.CreateBuilder<IOperation>();

            for (; i < source.Length && source[i] != ']'; i++)
            {
                switch (source[i])
                {
                    case '>':
                        offset++;
                        break;
                    case '<':
                        offset--;
                        break;
                    case '+':
                        builder.Add(new AddAssignOperation(new MemoryLocation(offset), 1));
                        break;
                    case '-':
                        builder.Add(new AddAssignOperation(new MemoryLocation(offset), -1));
                        break;
                    case '.':
                        builder.Add(new PutOperation(new MemoryLocation(offset)));
                        break;
                    case ',':
                        builder.Add(new ReadOperation(new MemoryLocation(offset)));
                        break;
                    case '[':
                        {
                            i++; // '['
                            var t = ParseUnit(source, offset, ref i);
                            builder.Add(new RoopUnitOperation(t.operations, t.ptrChange, new MemoryLocation(offset)));
                            break;
                        }
                    case ']':   // Unreachable
                    default:
                        // Do nothing
                        break;
                }
            }

            return (builder.ToImmutable(), offset);
        }
    }
}
