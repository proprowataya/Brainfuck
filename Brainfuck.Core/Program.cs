using System.Collections.Immutable;

namespace Brainfuck.Core
{
    public class Program
    {
        public string Source { get; }
        public ImmutableArray<IOperation> Operations { get; }

        internal Program(string source, ImmutableArray<IOperation> operations)
        {
            Source = source;
            Operations = operations;
        }
    }
}
