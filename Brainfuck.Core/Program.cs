using System.Collections.Immutable;

namespace Brainfuck.Core
{
    public class Program
    {
        public string Source { get; }
        public ImmutableArray<int> Dests { get; }

        internal Program(string source, ImmutableArray<int> dests)
        {
            Source = source;
            Dests = dests;
        }
    }
}
