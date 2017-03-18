using Brainfuck.Core;
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
            var srcs = new List<string>();
            var unknownCommands = new List<string>();
            bool nologo = false;

            // Parse command line arguments
            foreach (var arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    switch (arg)
                    {
                        case "--nologo":
                            nologo = true;
                            break;
                        default:
                            unknownCommands.Add(arg);
                            break;
                    }
                }
                else
                {
                    srcs.Add(arg);
                }
            }

            if (!nologo)
            {
                Console.WriteLine("Brainfuck .NET Compiler");
                Console.WriteLine();
            }

            if (unknownCommands.Count > 0)
            {
                foreach (var item in unknownCommands)
                {
                    Console.WriteLine($"Warning: Unknown command '{item}'");
                }
            }

            if (srcs.Count == 0)
            {
                Console.WriteLine("Error: Please specify Brainfuck code");
                Environment.Exit(-1);
            }

            // Compile each code
            foreach (var src in srcs)
            {
                // Read and parse code
                string srcFileName = Path.GetFileName(src);
                string code = File.ReadAllText(src);
                Brainfuck.Core.Program program = Parser.Parse(code).Optimize();
                string outFilename = Path.ChangeExtension(srcFileName, "exe");

                // Generate executable code
                var assemblyName = new AssemblyName(DefaultAssemblyName);
                var ab = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
                var mb = ab.DefineDynamicModule(ModuleName, outFilename);
                var tb = mb.DefineType(ClassName, TypeAttributes.Class);
                var method = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, new[] { typeof(string[]) });
                var il = method.GetILGenerator();
                new ILCompiler(Setting.Default).CompileToIL(program, il);
                tb.CreateType();
                ab.SetEntryPoint(method);
                ab.Save(outFilename);

                Console.WriteLine($"Compiled: '{srcFileName}' --> '{outFilename}'");
            }
        }
    }
}
