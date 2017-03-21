using System.Collections.Immutable;

namespace Brainfuck.Core
{
    public class Module
    {
        public string Source { get; }
        public ImmutableArray<IOperation> Operations { get; }

        internal Module(string source, ImmutableArray<IOperation> operations)
        {
            Source = source;
            Operations = operations;
        }
    }
}
