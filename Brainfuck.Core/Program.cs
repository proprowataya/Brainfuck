using System.Collections.Immutable;

namespace Brainfuck.Core
{
    public class Program
    {
        public string Source { get; }
        public ImmutableArray<Operation> Operations { get; }

        internal Program(string source, ImmutableArray<Operation> operations)
        {
            Source = source;
            Operations = operations;
        }
    }
}
