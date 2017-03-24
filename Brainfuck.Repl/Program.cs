﻿using Brainfuck.Core;
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
            else if (command.FileNames.Count == 0)
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
}
