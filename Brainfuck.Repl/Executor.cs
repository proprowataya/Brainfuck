using Brainfuck.Core;
using System.IO;
using System.Linq;

namespace Brainfuck.Repl
{
    internal class Executor : Program
    {
        public Executor(Setting setting, CommandLineArgument command) : base(setting, command)
        { }

        public override void Run()
        {
            // TODO: Currently we execute first source
            string source = File.ReadAllText(command.FileNames.First());
            if (EmitCodeIfNecessary(source))
            {
                // If we emit pseudo code, we don't execute code.
                return;
            }

            RunDefault(source, printHeader: false);
        }
    }
}