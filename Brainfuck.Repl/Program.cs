﻿using Brainfuck.Core;
using Brainfuck.Core.Analysis;
using Brainfuck.Core.ILGeneration;
using Brainfuck.Core.Interpretation;
using Brainfuck.Core.LowLevel;
using Brainfuck.Core.Optimization;
using Brainfuck.Core.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
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

        protected void RunDefault(string source, bool printHeader)
        {
            if (command.Engine == CommandLineArgument.ExecutionEngine.Interpreter || command.StepExecution)
            {
                RunByInterpreter(source, printHeader);
            }
            else
            {
                RunByILCompiler(source, printHeader);
            }
        }

        protected void RunByILCompiler(string source, bool printHeader, bool? overrideUnsafeCode = null)
        {
            bool unsafeCode = overrideUnsafeCode ?? this.setting.UnsafeCode;
            Setting setting = this.setting with { UnsafeCode = unsafeCode };    // overwrite

            StartProgram(() =>
            {
                Module module = ParseSource(source, Favor.ILSafe);
                ILCompiler compiler = new ILCompiler(setting);
                Action action = compiler.Compile(module);
                return action;
            }, printHeader ? $"===== JIT (System.Reflection.Emit{(unsafeCode ? ", unsafe" : "")}) =====" : null);
        }

        protected void RunByInterpreter(string source, bool printHeader, bool? overrideUnsafeCode = null)
        {
            bool unsafeCode = overrideUnsafeCode ?? (this.setting.UnsafeCode && !command.StepExecution);
            Setting setting = this.setting with { UnsafeCode = unsafeCode };    // overwrite
            if (command.StepExecution)
            {
                setting = setting with { BufferSize = 1, UseDynamicBuffer = true };
            }

            StartProgram(() =>
            {
                Module module = ParseSource(source, Favor.Interpreter);
                ImmutableArray<LowLevelOperation> operations = module.Root.ToLowLevel(setting);
                CancellationToken token = CancellationToken.None;
                Interpreter interpreter = new Interpreter(setting);

                if (command.StepExecution)
                {
                    var cts = new CancellationTokenSource();
                    token = cts.Token;

                    interpreter.OnStepStart += arg =>
                    {
                        PrintOnStepStartEventArgs(arg, operations);
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
                        interpreter.Execute(operations, token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Do nothing
                    }
                };
            }, printHeader ? $"===== Interpreter{(unsafeCode ? " (unsafe)" : "")} =====" : null);
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
                module = new Optimizer(setting with { Favor = favor }).Optimize(module);
            }

            return module;
        }

        protected bool EmitCodeIfNecessary(string source)
        {
            Module module = ParseSource(source, Favor.Default);

            if (command.EmitPseudoCode)
            {
                Console.Error.WriteLine();
                Console.Out.WriteLine("/**************");
                Console.Out.WriteLine(" * Pseudo Code");
                Console.Out.WriteLine(" **************/");
                Console.Out.WriteLine(module.ToPseudoCode());
                Console.Error.WriteLine();
            }
            else if (command.EmitLowLevelIntermediationCode)
            {
                Console.Error.WriteLine();
                var operations = module.Root.ToLowLevel(setting with { UseDynamicBuffer = true });

                for (int i = 0; i < operations.Length; i++)
                {
                    Console.Out.WriteLine($"{i,4}  {operations[i]}");
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        protected static void PrintOnStepStartEventArgs(OnStepStartEventArgs args, ImmutableArray<LowLevelOperation> operations)
        {
            Console.Error.Write($"{args.Step,4}: {("[" + operations[args.ProgramPointer] + "]").PadRight(24)} ");

            Console.Error.Write("Buffer = { ");
            for (int i = 0; i < args.Buffer.Length; i++)
            {
                if (i > 0)
                {
                    Console.Error.Write(", ");
                }

                if (i == args.BufferPointer)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }

                Console.Error.Write(args.Buffer.GetValue(i));
                Console.ResetColor();
            }
            Console.Error.WriteLine(" }");
        }
    }
}
