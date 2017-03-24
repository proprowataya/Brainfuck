using Brainfuck.Core;
using System.IO;

namespace Brainfuck.Repl
{
    internal class Executor : Program
    {
        public Executor(Setting setting, CommandLineArgument command) : base(setting, command)
        { }

        public override void Run()
        {
            string source = File.ReadAllText(command.FileName);
            if (EmitPseudoCodeIfNecessary(source))
            {
                // If we emit pseudo code, we don't execute code.
                return;
            }

            if (command.StepExecution)
            {
                RunByInterpreter(source, printHeader: false);
            }
            else
            {
                RunByILUnsafeCompiler(source, printHeader: false);
            }
        }
    }
}