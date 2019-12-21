using Brainfuck.Core;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace Brainfuck.Repl
{
    internal class CommandLineArgument
    {
        public enum ExecutionEngine
        {
            JIT, Interpreter,
        }

        public IReadOnlyList<string> FileNames { get; private set; } = null;
        public Type ElementType { get; private set; } = typeof(Int32);
        public bool Optimize { get; private set; } = true;
        public bool CheckRange { get; private set; } = false;
        public ExecutionEngine Engine { get; private set; } = ExecutionEngine.JIT;
        public bool Help { get; private set; } = false;
        public bool Silent { get; private set; } = false;
        public bool StepExecution { get; private set; } = false;
        public bool DebugMode { get; private set; } = false;
        public bool EmitPseudoCode { get; private set; } = false;
        public bool EmitLowLevelIntermediationCode { get; private set; } = false;

        public static CommandLineArgument Parse(string[] args, out Setting setting)
        {
            try
            {
                var arguments = new CommandLineArgument();
                arguments.ParseInternal(args, out setting);
                return arguments;
            }
            catch
            {
                setting = Setting.Default;
                return null;
            }
        }

        private void ParseInternal(string[] args, out Setting setting)
        {
            FileNames = Option.Parse(args);
            setting = Setting.Default.WithElementType(ElementType)
                                     .WithUnsafeCode(!CheckRange);
        }

        public static void PrintHelp()
        {
            Console.Error.WriteLine("Usage: dotnet Brainfuck.Repl.dll [source-path] [options]");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Options:");
            new CommandLineArgument().Option.WriteOptionDescriptions(Console.Error);
        }

        private OptionSet Option => new OptionSet()
        {
            {
                "od",
                "Disable optimization.",
                v => Optimize = (v == null)
            },
            {
                "c|checked",
                "Detect out of range access. "
                    + "If this option is NOT specified, out of range access causes an unpredictable state. "
                    + "Default behavior is 'unchecked'(unsafe).",
                v => CheckRange = (v != null)
            },
            {
                "i|int=",
                "Specify integer size (VALUE: 8, 16, 32, 64).",
                (int v) =>
                {
                    ElementType = v switch
                    {
                        8 => typeof(Byte),
                        16 => typeof(Int16),
                        32 => typeof(Int32),
                        64 => typeof(Int64),
                        _ => throw new OptionException($"Invalid size: {v}", "size"),
                    };
                }
            },
#if false
            {
                "silent",
                "Don't show any messages (except for errors).",
                v => Silent = (v != null)
            },
#endif  
            {
                "e|engine=",
                "Specify execution engine. If step execution is enabled, this value will be ignored. "
                    + "By default, JIT engine will be used."
                    + $"\n(VALUE: {string.Join(", ", Enum.GetNames(typeof(ExecutionEngine)))})",
                v =>
                {
                    if (Enum.TryParse<ExecutionEngine>(v, out var engine))
                    {
                        Engine = engine;
                    }
                    else
                    {
                        throw new OptionException($"Unknown engine '{v}'.", "engine");
                    }
                }
            },
            {
                "s|step",
                "Enable step execution.",
                v => StepExecution = (v != null)
            },
            {
                "d|debug",
                "Enable debug mode.",
                v => DebugMode = (v != null)
            },
            {
                "p|emit-pseudo-code",
                "Emit pseudo code (C-like style).",
                v => EmitPseudoCode = (v != null)
            },
            {
                "l|emit-low-level-code",
                "Emit low-level intermediate representation code.",
                v => EmitLowLevelIntermediationCode = (v != null)
            },
            {
                "h|help",
                "Show help (this message).",
                v => Help = (v != null)
            },
        };
    }
}
