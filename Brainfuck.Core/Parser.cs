using Brainfuck.Core.Syntax;
using System.Collections.Immutable;

namespace Brainfuck.Core
{
    public static class Parser
    {
        public static Module Parse(string source)
        {
            int i = 0;
            var t = ParseUnit(source, 0, ref i);
            return new Module(source, new BlockUnit(t.statements, t.offsetChange));
        }

        private static (ImmutableArray<IStatement> statements, int offsetChange) ParseUnit(string source, int offset, ref int i)
        {
            int firstOffset = offset;
            var builder = ImmutableArray.CreateBuilder<IStatement>();

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
                        builder.Add(new AssignStatement(
                                        new MemoryLocation(offset),
                                        new AddExpression(new MemoryAccessExpression(new MemoryLocation(offset)), ConstExpression.One)));
                        break;
                    case '-':
                        builder.Add(new AssignStatement(
                                        new MemoryLocation(offset),
                                        new AddExpression(new MemoryAccessExpression(new MemoryLocation(offset)), ConstExpression.MinusOne)));
                        break;
                    case '.':
                        builder.Add(new PutStatement(new MemoryAccessExpression(new MemoryLocation(offset))));
                        break;
                    case ',':
                        builder.Add(new AssignStatement(new MemoryLocation(offset), GetExpression.Instance));
                        break;
                    case '[':
                        {
                            i++; // '['
                            var t = ParseUnit(source, offset, ref i);
                            builder.Add(new RoopUnit(t.statements, t.offsetChange, new MemoryLocation(offset)));
                            break;
                        }
                    case ']':
                    // Unreachable
                    default:
                        // Do nothing
                        break;
                }
            }

            return (builder.ToImmutable(), offset - firstOffset);
        }
    }
}
