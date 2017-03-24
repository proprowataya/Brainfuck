using Brainfuck.Core;
using Brainfuck.Core.Syntax;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Brainfuck.Repl
{
    internal abstract class Program
    {
        protected readonly Setting setting;
        protected readonly CommandLineArgument command;

        public Program(Setting setting, CommandLineArgument command)
        {
            this.setting = setting;
            this.command = command;
        }

        public abstract void Run();

        public static void Main(string[] args)
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
                new Repl(setting, command).Run();
            }
            else
            {
                new Executor(setting, command).Run();
            }
        }

        protected void RunByILUnsafeCompiler(string source, bool printHeader)
        {
            StartProgram(() =>
            {
                Module module = ParseSource(source, Favor.ILUnsafe);
                ILCompiler compiler = new ILCompiler(setting.WithUnsafeCode(true));
                Action action = compiler.Compile(module);
                return action;
            }, printHeader ? "===== Compiler (System.Reflection.Emit, unsafe) =====" : null);
        }

        protected void RunByILCompiler(string source, bool printHeader)
        {
            StartProgram(() =>
            {
                Module module = ParseSource(source, Favor.ILSafe);
                ILCompiler compiler = new ILCompiler(setting);
                Action action = compiler.Compile(module);
                return action;
            }, printHeader ? "===== Compiler (System.Reflection.Emit) =====" : null);
        }

        protected void RunByInterpreter(string source, bool printHeader)
        {
            StartProgram(() =>
            {
                Module module = ParseSource(source, Favor.Interpreter);
                Setting usingSetting = setting;

                if (command.StepExecution)
                {
                    usingSetting = usingSetting.WithBufferSize(1);
                }

                var cts = new CancellationTokenSource();
                Interpreter interpreter = new Interpreter(usingSetting);

                if (command.StepExecution)
                {
                    interpreter.OnStepStart += arg =>
                    {
                        PrintOnStepStartEventArgs(arg);
                        ConsoleKeyInfo key = Console.ReadKey();

                        if (key.Key == ConsoleKey.Escape)
                        {
                            cts.Cancel();
                        }
                    };
                }

                return () =>
                {
                    try
                    {
                        interpreter.Execute(module, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Do nothing
                    }
                };
            }, printHeader ? "===== Interpreter =====" : null);
        }

        private static void StartProgram(Func<Action> generator, string message)
        {
#if !DEBUG
            try
#endif
            {
                if (message != null)
                {
                    Console.Error.WriteLine(message);
                }

                Action action = generator();
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

        protected Module ParseSource(string source, Favor favor)
        {
            Module module = Parser.Parse(source);
            if (command.Optimize)
            {
                module = new Optimizer(setting.WithFavor(favor)).Optimize(module);
            }

            return module;
        }

        protected bool EmitPseudoCodeIfNecessary(string source)
        {
            if (command.EmitPseudoCode)
            {
                Console.Error.WriteLine();
                Console.Out.WriteLine("/**************");
                Console.Out.WriteLine(" * Pseudo Code");
                Console.Out.WriteLine(" **************/");
                Console.Out.WriteLine(ParseSource(source, Favor.Default).ToPseudoCode());
                Console.Error.WriteLine();
                return true;
            }

            return false;
        }

        protected static void PrintOnStepStartEventArgs(OnStepStartEventArgs args)
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
    }

    internal class CommandLineArgument
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
