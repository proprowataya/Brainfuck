using System;
using System.Diagnostics;
using System.Text;
using Brainfuck.Core;
using System.Threading;

namespace Brainfuck.Repl
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.WriteLine("Brainfuck Interpreter on .NET Core");
            Console.WriteLine();

            while (true)
            {
                string source = ReadCode();
                if (source == "exit")
                    break;
                Brainfuck.Core.Program program = Parser.Parse(source).Optimize();

                Run(() =>
                {
                    Compiler compiler = new Compiler(Setting.Default);
                    Action action = compiler.Compile(program);
                    action();
                }, "===== Compiler =====");

                Run(() =>
                {
                    var cts = new CancellationTokenSource();
                    Interpreter interpreter = new Interpreter(Setting.Default.WithBufferSize(1));
                    interpreter.OnStepStart += arg =>
                    {
                        PrintOnStepStartEventArgs(program, arg);
                        ConsoleKeyInfo key = Console.ReadKey();

                        if (key.Key == ConsoleKey.Escape)
                        {
                            cts.Cancel();
                        }
                    };

                    try
                    {
                        interpreter.Execute(program, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Do nothing
                    }
                }, "===== Interpreter (step execution) =====");
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

        private static void PrintOnStepStartEventArgs(Brainfuck.Core.Program program, OnStepStartEventArgs args)
        {
            Console.Write($"{args.Index,3}: {("[" + program.Operations[args.Index].ToString() + "]").PadRight(24)}");

            Console.Write("Buffer = { ");
            for (int i = 0; i < args.Buffer.Count; i++)
            {
                if (i > 0)
                {
                    Console.Write(", ");
                }

                if (i == args.Pointer)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }

                Console.Write(args.Buffer[i]);
                Console.ResetColor();
            }
            Console.WriteLine(" }");
        }
    }
}
