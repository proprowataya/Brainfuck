using Brainfuck.Core.Syntax;
using System.Collections.Immutable;

namespace Brainfuck.Core
{
    public class Module
    {
        public string Source { get; }
        public IStatement Root { get; }

        public Module(string source, IStatement root)
        {
            Source = source;
            Root = root;
        }
    }
}
