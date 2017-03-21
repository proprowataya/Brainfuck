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
            Stack<List<IOperation>> stack = new Stack<List<IOperation>>();
            List<IOperation> list = new List<IOperation>();

            stack.Push(list);

            for (int i = 0; i < source.Length; i++)
            {
                switch (source[i])
                {
                    case '>':
                        list.Add(new AddPtr(1));
                        break;
                    case '<':
                        list.Add(new AddPtr(-1));
                        break;
                    case '+':
                        list.Add(new AddValue(MemoryLocation.Zero, 1));
                        break;
                    case '-':
                        list.Add(new AddValue(MemoryLocation.Zero, -1));
                        break;
                    case '.':
                        list.Add(new Put(MemoryLocation.Zero));
                        break;
                    case ',':
                        list.Add(new Read(MemoryLocation.Zero));
                        break;
                    case '[':
                        {
                            var newList = new List<IOperation>();
                            stack.Push(newList);
                            list = newList;
                            break;
                        }
                    case ']':
                        {
                            stack.Pop();
                            stack.Peek().Add(new Roop(ImmutableArray.CreateRange(list)));
                            list = stack.Peek();
                            break;
                        }
                    default:
                        // Do nothing
                        break;
                }
            }

            if (stack.Count != 1)
            {
                // TODO
                throw new InvalidOperationException();
            }

            Debug.Assert(stack.Peek() == list);
            return new Module(source, ImmutableArray.CreateRange(list));
        }
    }
}
