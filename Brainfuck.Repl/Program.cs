using System;
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

                Interpreter.Execute(source);
                Console.WriteLine();
                Console.WriteLine();
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
    }
}
