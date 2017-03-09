using Brainfuck.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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

            CommandLineArgument command = CommandLineArgument.Parse(args);

            if (command == null || command.PrintHelp)
            {
                PrintHelp();
                if (command == null)
                {
                    Environment.Exit(-1);
                }
            }
            else if (command.FileName == null)
            {
                Repl(command);
            }
            else
            {
                Execute(command);
            }
        }

        private static void Repl(CommandLineArgument command)
        {
            while (true)
            {
                string source = ReadCode();
                if (source == "exit")
                    break;
                Brainfuck.Core.Program program = Parser.Parse(source);
                if (command.Optimize)
                    program = program.Optimize();

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

                    if (command.StepExecution)
                    {
                        interpreter.OnStepStart += arg =>
                        {
                            PrintOnStepStartEventArgs(program, arg);
                            ConsoleKeyInfo key = Console.ReadKey();

                            if (key.Key == ConsoleKey.Escape)
                            {
                                cts.Cancel();
                            }
                        };
                    }

                    try
                    {
                        interpreter.Execute(program, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Do nothing
                    }
                }, "===== Interpreter =====");
            }
        }

        private static void Execute(CommandLineArgument command)
        {
            string source = File.ReadAllText(command.FileName);
            Brainfuck.Core.Program program = Parser.Parse(source);
            if (command.Optimize)
            {
                program = program.Optimize();
            }

            Setting setting = Setting.Default;

            if (!command.StepExecution)
            {
                Compiler compiler = new Compiler(setting);
                Action action = compiler.Compile(program);

                Stopwatch sw = Stopwatch.StartNew();
                action();
                sw.Stop();

                Console.WriteLine();
                Console.WriteLine($"Elapsed {sw.Elapsed}");
            }
            else
            {
                Interpreter interpreter = new Interpreter(setting.WithBufferSize(1));
                interpreter.OnStepStart += arg =>
                {
                    PrintOnStepStartEventArgs(program, arg);
                    Console.ReadKey();
                };

                interpreter.Execute(program);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  Unavailable");
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

        private class CommandLineArgument
        {
            public string FileName { get; set; } = null;
            public bool Optimize { get; set; } = false;
            public bool PrintHelp { get; set; } = false;
            public bool StepExecution { get; set; } = false;

            public static CommandLineArgument Parse(string[] args)
            {
                CommandLineArgument result = new CommandLineArgument();

                foreach (var arg in args)
                {
                    if (arg.StartsWith("-"))
                    {
                        switch (arg)
                        {
                            case "-o":
                            case "--optimize":
                                result.Optimize = true;
                                break;
                            case "-h":
                            case "--help":
                                result.PrintHelp = true;
                                break;
                            case "-s":
                            case "--step":
                                result.StepExecution = true;
                                break;
                            default:
                                Console.WriteLine($"Error: Unknown command '{arg}'");
                                return null;
                        }
                    }
                    else
                    {
                        result.FileName = arg;
                    }
                }

                return result;
            }
        }
    }
}
