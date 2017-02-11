using System.Collections.Generic;
using System.Collections.Immutable;

namespace Brainfuck.Core
{
    public static class Parser
    {
        public static Program Parse(string source)
        {
            var bracketStack = new Stack<int>();
            var openingDest = ImmutableArray.CreateBuilder<int>(source.Length);
            var closingDest = ImmutableArray.CreateBuilder<int>(source.Length);

            openingDest.Count = source.Length;
            closingDest.Count = source.Length;

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
                            openingDest[startAddress] = i;
                            closingDest[i] = startAddress;
                            break;
                        }
                }
            }

            return new Program(source, openingDest.MoveToImmutable(), closingDest.MoveToImmutable());
        }
    }
}
