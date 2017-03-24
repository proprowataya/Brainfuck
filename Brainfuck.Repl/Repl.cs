using Brainfuck.Core;
using System;
using System.Text;

namespace Brainfuck.Repl
{
    class Repl : Program
    {
        public Repl(Setting setting, CommandLineArgument command) : base(setting, command)
        { }

        public override void Run()
        {
            while (ReadCode() is string source && source != "exit")
            {
#if !DEBUG
                try
#endif
                {
                    EmitPseudoCodeIfNecessary(source);
                    RunByILUnsafeCompiler(source, printHeader: true);
                    RunByILCompiler(source, printHeader: true);
                    RunByInterpreter(source, printHeader: true);
                }
#if !DEBUG
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.ToString());
                    Console.Error.WriteLine();
                }
#endif
            }
        }

        private static string ReadCode()
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                Console.Error.Write("> ");
                string line = Console.ReadLine();
                if (line.Length == 0)
                    break;
                sb.Append(line);
            }

            return sb.ToString();
        }
    }
}
