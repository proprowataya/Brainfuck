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
            Console.Error.WriteLine("Brainfuck Interpreter on .NET Core");
            Console.Error.WriteLine();

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
#if !DEBUG
                try
#endif
                {
                    if (command.EmitPseudoCode)
                    {
                        Console.Error.WriteLine();
                        Console.Out.WriteLine("/***** Pseudo Code *****/");
                        Console.Out.WriteLine(ParseSource(source, command.Optimize, setting).ToPseudoCode());
                        Console.Error.WriteLine();
                    }

                    RunByILUnsafeCompiler(source, setting, optimize: command.Optimize, printHeader: true);
                    RunByILCompiler(source, setting, optimize: command.Optimize, printHeader: true);
                    RunByInterpreter(source, setting, optimize: command.Optimize, printHeader: true, stepExecution: command.StepExecution);
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

        private static void Execute(CommandLineArgument command, Setting setting)
        {
            string source = File.ReadAllText(command.FileName);
            Module module = ParseSource(source, command.Optimize, setting);
            if (command.EmitPseudoCode)
            {
                Console.Out.WriteLine("/***** Pseudo Code *****/");
                Console.Out.WriteLine(module.ToPseudoCode());
                return; // Don't execude code
            }

            if (command.StepExecution)
            {
                RunByInterpreter(source, setting, optimize: command.Optimize, printHeader: false, stepExecution: true);
            }
            else
            {
                RunByILUnsafeCompiler(source, setting, optimize: command.Optimize, printHeader: false);
            }
        }

        #region Runs

        private static void RunByILUnsafeCompiler(string source, Setting setting, bool optimize, bool printHeader)
        {
            Run(() =>
            {
                Module module = ParseSource(source, optimize, setting.WithFavor(Favor.ILUnsafe));
                ILCompiler compiler = new ILCompiler(setting.WithUnsafeCode(true));
                Action action = compiler.Compile(module);
                action();
            }, printHeader ? "===== Compiler (System.Reflection.Emit, unsafe) =====" : null);
        }

        private static void RunByILCompiler(string source, Setting setting, bool optimize, bool printHeader)
        {
            Run(() =>
            {
                Module module = ParseSource(source, optimize, setting.WithFavor(Favor.ILSafe));
                ILCompiler compiler = new ILCompiler(setting);
                Action action = compiler.Compile(module);
                action();
            }, printHeader ? "===== Compiler (System.Reflection.Emit) =====" : null);
        }

        private static void RunByInterpreter(string source, Setting setting, bool optimize, bool printHeader, bool stepExecution)
        {
            Run(() =>
            {
                Module module = ParseSource(source, optimize, setting.WithFavor(Favor.Interpreter));

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
#if !DEBUG
            try
#endif
            {
                if (message != null)
                {
                    Console.Error.WriteLine(message);
                }

                Stopwatch sw = Stopwatch.StartNew();
                action();
                sw.Stop();

                Console.Error.WriteLine();
                Console.Error.WriteLine($"Elapsed {sw.Elapsed}");
                Console.Error.WriteLine();
            }
#if !DEBUG
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                Console.Error.WriteLine();
            }
#endif
        }

        #endregion

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

        private static Module ParseSource(string source, bool optimize, Setting setting)
        {
            Module module = Parser.Parse(source);
            if (optimize)
            {
                module = new Optimizer(setting).Optimize(module);
            }

            return module;
        }

        private static void PrintOnStepStartEventArgs(Module module, OnStepStartEventArgs args)
        {
            Console.Error.Write($"{args.Step,4}: {("[" + args.Operation.GetType().Name + "]").PadRight(24)} ");

            Console.Error.Write("Buffer = { ");
            for (int i = 0; i < args.Buffer.Count; i++)
            {
                if (i > 0)
                {
                    Console.Error.Write(", ");
                }

                if (i == args.Pointer)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }

                Console.Error.Write(args.Buffer[i]);
                Console.ResetColor();
            }
            Console.Error.WriteLine(" }");
        }

        private class CommandLineArgument
        {
            public string FileName { get; } = null;
            public bool Optimize { get; } = true;
            public bool Help { get; } = false;
            public bool StepExecution { get; } = false;
            public bool EmitPseudoCode { get; } = false;

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
                            case "-p":
                            case "--pseudo":
                                EmitPseudoCode = true;
                                break;
                            default:
                                Console.Error.WriteLine($"Error: Unknown command '{arg}'");
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
                    new[]{ "-p, --pseudo", "Emit pseudo code" },
                };

                int maxCommandLength = commands.Max(c => c[0].Length);

                Console.Error.WriteLine("Usage: dotnet Brainfuck.Repl.dll [source-path] [options]");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Options:");
                foreach (var command in commands)
                {
                    Console.Error.WriteLine($"  {command[0].PadRight(maxCommandLength)}  {command[1]}");
                }
            }
        }
    }
}
