using Brainfuck.Core;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Brainfuck.Compiler
{
    class Program
    {
        private const string DefaultAssemblyName = "Brainfuck";
        private const string ModuleName = "Brainfuck";
        private const string ClassName = "Brainfuck";

        static void Main(string[] args)
        {
            // Options
            bool Optimize = true;
            bool CheckRange = false;
            Type ElementType = typeof(Int32);
            bool Silent = false;
            bool Help = false;

            // Parse command line arguments
            OptionSet option = new OptionSet()
            {
                { "od", "Disable optimization.", v => Optimize = (v == null) },
                { "c|checked", "Detect out of range access. If this option is NOT specified, out of range access causes an unpredictable state. Default behavior is 'unchecked'(unsafe).", v => CheckRange = (v != null) },
                { "s|size=", "Specify size of buffer elements (VALUE: 8, 16, 32, 64).", (int v) =>
                    {
                        switch (v)
                        {
                            case 8:
                                ElementType = typeof(Byte);
                                break;
                            case 16:
                                ElementType = typeof(Int16);
                                break;
                            case 32:
                                ElementType = typeof(Int32);
                                break;
                            case 64:
                                ElementType = typeof(Int64);
                                break;
                            default:
                                throw new OptionException($"Invalid size: {v}", "size");
                        }
                    }
                },
                { "silent", "Don't show any messages (except for errors).", v => Silent = (v != null) },
                { "h|help", "Show help (this message).", v => Help = (v != null) },
            };

            List<string> srcs = null;
            List<string> errors = new List<string>();
            try
            {
                srcs = option.Parse(args);
            }
            catch (OptionException e)
            {
                errors.Add(e.Message);
            }

            if (!Silent)
            {
                Console.WriteLine("Brainfuck .NET Desktop Compiler");
            }

            if (Help)
            {
                Console.WriteLine();
                Console.WriteLine("Usage: bfc source-path+ [options*]");
                Console.WriteLine();
                option.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (srcs == null || srcs.Count == 0)
            {
                errors.Add("Brainfuck code is not specified. Type '--help' for more information.");
            }

            if (errors.Count > 0)
            {
                Console.WriteLine();

                foreach (var error in errors)
                {
                    Console.WriteLine("Error: " + error);
                }

                Environment.ExitCode = -1;
                return;
            }

            // Print options
            if (!Silent)
            {
                Console.WriteLine("  Option : " + new { Optimize, CheckRange, ElementType, Silent, Help });
                Console.WriteLine();
            }

            // Create setting
            Setting setting = Setting.Default.WithUnsafeCode(!CheckRange).WithElementType(ElementType);

            // Compile each code
            foreach (var src in srcs)
            {
                try
                {
                    // Read and parse code
                    string srcFileName = Path.GetFileName(src);
                    string code = File.ReadAllText(src);
                    string outFilename = Path.ChangeExtension(srcFileName, "exe");
                    Brainfuck.Core.Program program = Parser.Parse(code);
                    if (Optimize)
                    {
                        program = program.Optimize();
                    }

                    // Generate executable code
                    var assemblyName = new AssemblyName(DefaultAssemblyName);
                    var ab = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
                    var mb = ab.DefineDynamicModule(ModuleName, outFilename);
                    var tb = mb.DefineType(ClassName, TypeAttributes.Class);
                    var method = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, new[] { typeof(string[]) });
                    var il = method.GetILGenerator();
                    new ILCompiler(setting).CompileToIL(program, il);
                    tb.CreateType();
                    ab.SetEntryPoint(method);
                    ab.Save(outFilename);

                    if (!Silent)
                    {
                        Console.WriteLine($"Compiled: '{srcFileName}' --> '{outFilename}'");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error('{src}'): {e.Message}");
                }
            }
        }
    }
}
