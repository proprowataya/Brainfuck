using System.Collections.Generic;
using System.Collections.Immutable;

namespace Brainfuck.Core
{
    public static class Parser
    {
        public static Program Parse(string source)
        {
            var bracketStack = new Stack<int>();
            var dests = ImmutableArray.CreateBuilder<int>(source.Length);
            dests.Count = source.Length;

            for (int i = 0; i < source.Length; i++)
            {
                switch (source[i])
                {
                    case '[':
                        {
                            bracketStack.Push(i);
                            break;
                        }
                    case ']':
                        {
                            int startAddress = bracketStack.Pop();
                            dests[startAddress] = i;
                            dests[i] = startAddress;
                            break;
                        }
                }
            }

            return new Program(source, dests.MoveToImmutable());
        }
    }
}
