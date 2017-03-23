using Brainfuck.Core.Syntax;

namespace Brainfuck.Core
{
    public class Module
    {
        public string Source { get; }
        public BlockUnitOperation Root { get; }

        internal Module(string source, BlockUnitOperation root)
        {
            Source = source;
            Root = root;
        }
    }
}
