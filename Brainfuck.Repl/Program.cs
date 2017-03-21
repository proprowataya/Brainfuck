using Brainfuck.Core;
using Brainfuck.Core.Syntax;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            CommandLineArgument command = CommandLineArgument.Parse(args, out var setting);

            if (command == null || command.Help)
            {
                CommandLineArgument.PrintHelp();
                if (command == null)
                {
                    Environment.Exit(-1);
                }
            }
            else if (command.FileName == null)
            {
                Repl(command, setting);
            }
            else
            {
                Execute(command, setting);
            }
        }

        private static void Repl(CommandLineArgument command, Setting setting)
        {
            while (ReadCode() is string source && source != "exit")
            {
                try
                {
                    Module module = ParseSource(source, command.Optimize);
                    if (command.ShowPseudoCode)
                    {
                        Console.WriteLine("/***** Pseudo Code *****/");
                        Console.WriteLine(module.ToPseudoCode());
                        Console.WriteLine();
                    }

                    RunByILUnsafeCompiler(module, setting, printHeader: true);
                    RunByILCompiler(module, setting, printHeader: true);
                    RunByInterpreter(module, setting, printHeader: true, stepExecution: command.StepExecution);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.WriteLine();
                }
            }
        }

        private static void Execute(CommandLineArgument command, Setting setting)
        {
            string source = File.ReadAllText(command.FileName);
            Module module = ParseSource(source, command.Optimize);
            if (command.ShowPseudoCode)
            {
                Console.WriteLine("/***** Pseudo Code *****/");
                Console.WriteLine(module.ToPseudoCode());
                Console.WriteLine();
                return; // Don't execude code
            }

            if (command.StepExecution)
            {
                RunByInterpreter(module, setting, printHeader: false, stepExecution: true);
            }
            else
            {
                RunByILUnsafeCompiler(module, setting, printHeader: false);
            }
        }

        #region Runs

        private static void RunByILUnsafeCompiler(Module module, Setting setting, bool printHeader)
        {
            Run(() =>
            {
                ILCompiler compiler = new ILCompiler(setting.WithUnsafeCode(true));
                Action action = compiler.Compile(module);
                action();
            }, printHeader ? "===== Compiler (System.Reflection.Emit, unsafe) =====" : null);
        }

        private static void RunByILCompiler(Module module, Setting setting, bool printHeader)
        {
            Run(() =>
            {
                ILCompiler compiler = new ILCompiler(setting);
                Action action = compiler.Compile(module);
                action();
            }, printHeader ? "===== Compiler (System.Reflection.Emit) =====" : null);
        }

        private static void RunByInterpreter(Module module, Setting setting, bool printHeader, bool stepExecution)
        {
            Run(() =>
            {
                if (stepExecution)
                {
                    setting = setting.WithBufferSize(1);
                }

                var cts = new CancellationTokenSource();
                Interpreter interpreter = new Interpreter(setting);

                if (stepExecution)
                {
                    interpreter.OnStepStart += arg =>
                    {
                        PrintOnStepStartEventArgs(module, arg);
                        ConsoleKeyInfo key = Console.ReadKey();

                        if (key.Key == ConsoleKey.Escape)
                        {
                            cts.Cancel();
                        }
                    };
                }

                try
                {
                    interpreter.Execute(module, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Do nothing
                }
            }, printHeader ? "===== Interpreter =====" : null);
        }

        private static void Run(Action action, string message)
        {
            try
            {
                if (message != null)
                {
                    Console.WriteLine(message);
                }

                Stopwatch sw = Stopwatch.StartNew();
                action();
                sw.Stop();

                Console.WriteLine();
                Console.WriteLine($"Elapsed {sw.Elapsed}");
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine();
            }
        }

        #endregion

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

        private static Module ParseSource(string source, bool optimize)
        {
            Module module = Parser.Parse(source);
            if (optimize)
            {
                module = module.Optimize();
            }

            return module;
        }

        private static void PrintOnStepStartEventArgs(Module module, OnStepStartEventArgs args)
        {
            Console.Write($"{args.Step,4}: {("[" + args.Operation + "]").PadRight(24)}");

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
            public string FileName { get; } = null;
            public bool Optimize { get; } = true;
            public bool Help { get; } = false;
            public bool StepExecution { get; } = false;
            public bool ShowPseudoCode { get; } = true;

            public static CommandLineArgument Parse(string[] args, out Setting setting)
            {
                CommandLineArgument result = new CommandLineArgument(args, out setting, out bool success);
                return success ? result : null;
            }

            private CommandLineArgument(string[] args, out Setting setting, out bool success)
            {
                setting = Setting.Default;
                success = true;

                foreach (var arg in args)
                {
                    if (arg.StartsWith("-"))
                    {
                        switch (arg)
                        {
                            case "-od":
                            case "--optimize=disable":
                                Optimize = false;
                                break;
                            case "-h":
                            case "--help":
                                Help = true;
                                break;
                            case "-s":
                            case "--step":
                                StepExecution = true;
                                break;
                            default:
                                Console.WriteLine($"Error: Unknown command '{arg}'");
                                success = false;
                                break;
                        }
                    }
                    else
                    {
                        FileName = arg;
                    }
                }
            }

            public static void PrintHelp()
            {
                string[][] commands =
                {
                    new[]{ "-od, --optimize=disable", "Disable optimization" },
                    new[]{ "-h, --help", "Print usage (this message)" },
                    new[]{ "-s, --step", "Enable step execution" },
                };

                int maxCommandLength = commands.Max(c => c[0].Length);

                Console.WriteLine("Usage: dotnet Brainfuck.Repl.dll [source-path] [options]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                foreach (var command in commands)
                {
                    Console.WriteLine($"  {command[0].PadRight(maxCommandLength)}  {command[1]}");
                }
            }
        }
    }
}
