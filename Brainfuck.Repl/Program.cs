using System;
using System.Diagnostics;
using System.Text;
using Brainfuck.Core;

namespace Brainfuck.Repl
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Brainfuck Interpreter on .NET Core");
            Console.WriteLine();

            while (true)
            {
                string source = ReadCode();
                if (source == "exit")
                    break;
                Brainfuck.Core.Program program = Parser.Parse(source);

                Run(() => Interpreter.Execute(program), "Run in interpreter");
                Run(() =>
                {
                    Compiler compiler = new Compiler(CompilerSetting.Default);
                    Action action = compiler.Compile(program);
                    action();
                }, "Run using compiler");
            }
        }

        private static string ReadCode()
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                Console.Write("> ");
                string line = Console.ReadLine();
                if (line.Length == 0)
                    break;
                sb.Append(line);
            }

            return sb.ToString();
        }

        private static TimeSpan Run(Action action, string message)
        {
            Console.WriteLine(message);

            Stopwatch sw = Stopwatch.StartNew();
            action();
            sw.Stop();

            Console.WriteLine();
            Console.WriteLine($"Elapsed {sw.Elapsed}");
            Console.WriteLine();
            return sw.Elapsed;
        }
    }
}
