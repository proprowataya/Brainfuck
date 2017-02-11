using System.Collections.Immutable;

namespace Brainfuck.Core
{
    public class Program
    {
        public string Source { get; }
        public ImmutableArray<int> OpeningDest { get; }
        public ImmutableArray<int> ClosingDest { get; }

        internal Program(string source, ImmutableArray<int> openingDest, ImmutableArray<int> closingDest)
        {
            Source = source;
            OpeningDest = openingDest;
            ClosingDest = closingDest;
        }
    }
}
