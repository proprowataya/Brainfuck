using Brainfuck.Core;
using Brainfuck.Core.ILGeneration;
using Brainfuck.Core.LowLevel;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Brainfuck.Test
{
    public class ExecutionTest
    {
        [Theory, MemberData(nameof(ExecutionCase))]
        public void HelloWorldTest(object description, Action<Module> action, bool optimize)
        {
            const string HelloWorld = "Hello World!";
            const string Code = ">+++++++++[<++++++++>-]<.>+++++++[<++++>-]<+.+++++++..+++.[-]>++++++++[<++++>-]<.>+++++++++++[<+++++>-]<.>++++++++[<+++>-]<.+++.------.--------.[-]>++++++++[<++++>-]<+.[-]++++++++++.";
            Assert.Equal(HelloWorld, Test(action, Code, optimize));
        }

        [Theory, MemberData(nameof(ExecutionCase))]
        public void HelloWorldWithErrorsTest(object description, Action<Module> action, bool optimize)
        {
            const string HelloWorld = "Hello World!";
            const string Code = ">++++  + + + ++111111[<1+1+1+1+1+1+1+1+1>1-1]1<1.1>1+1+1+++++[<++++>-]<+.+++++++..+++.[-]>++++++++[<++++>-]<.>+++++++++++[<+++++>-]<.>++++++++[<+++>-]<.+++.------.--------.[-]>++++++++[<++++>-]<+.[-]+++++++++11+.";
            Assert.Equal(HelloWorld, Test(action, Code, optimize));
        }

        [Theory, MemberData(nameof(ExecutionCase))]
        public void EchoTest(object description, Action<Module> action, bool optimize)
        {
            const string InputString = "This is a test string.\0";
            const string Code = "+[,.]";
            Assert.Equal(InputString, Test(action, Code, optimize, InputString));
        }

        [Theory, MemberData(nameof(ExecutionCase))]
        public void EchoTest2(object description, Action<Module> action, bool optimize)
        {
            const string InputString = "This is a test string.\0";
            const string Code = ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>+[,.]";
            Assert.Equal(InputString, Test(action, Code, optimize, InputString));
        }

        [Theory, MemberData(nameof(ExecutionCase))]
        public void PrimeTest(object description, Action<Module> action, bool optimize)
        {
            // http://esoteric.sange.fi/brainfuck/bf-source/prog/PRIME.BF
            const string Code = @"
>++++++++[<++++++++>-]<++++++++++++++++.[-]>++++++++++[<++++++++++>-]<++++++++++++++.[-]>+++++++
+++[<++++++++++>-]<+++++.[-]>++++++++++[<++++++++++>-]<+++++++++.[-]>++++++++++[<++++++++++>-]<+
.[-]>++++++++++[<++++++++++>-]<+++++++++++++++.[-]>+++++[<+++++>-]<+++++++.[-]>++++++++++[<+++++
+++++>-]<+++++++++++++++++.[-]>++++++++++[<++++++++++>-]<++++++++++++.[-]>+++++[<+++++>-]<++++++
+.[-]>++++++++++[<++++++++++>-]<++++++++++++++++.[-]>++++++++++[<++++++++++>-]<+++++++++++.[-]>+
++++++[<+++++++>-]<+++++++++.[-]>+++++[<+++++>-]<+++++++.[-]+[->,----------[<+>-----------------
--------------------->[>+>+<<-]>>[<<+>>-]<>>>+++++++++[<<<[>+>+<<-]>>[<<+>>-]<[<<+>>-]>>-]<<<[-]
<<[>+<-]]<]>>[<<+>>-]<<>+<-[>+[>+>+<<-]>>[<<+>>-]<>+<-->>>>>>>>+<<<<<<<<[>+<-<[>>>+>+<<<<-]>>>>[
<<<<+>>>>-]<<<>[>>+>+<<<-]>>>[<<<+>>>-]<<<<>>>[>+>+<<-]>>[<<+>>-]<<<[>>>>>+<<<[>+>+<<-]>>[<<+>>-
]<[>>[-]<<-]>>[<<<<[>+>+<<-]>>[<<+>>-]<>>>-]<<<-<<-]+>>[<<[-]>>-]<<>[-]<[>>>>>>[-]<<<<<<-]<<>>[-
]>[-]<<<]>>>>>>>>[-<<<<<<<[-]<<[>>+>+<<<-]>>>[<<<+>>>-]<<<>>[>+<-]>[[>+>+<<-]>>[<<+>>-]<>+++++++
++<[>>>+<<[>+>[-]<<-]>[<+>-]>[<<++++++++++>>-]<<-<-]+++++++++>[<->-]<[>+<-]<[>+<-]<[>+<-]>>>[<<<
+>>>-]<>+++++++++<[>>>+<<[>+>[-]<<-]>[<+>-]>[<<++++++++++>>>+<-]<<-<-]>>>>[<<<<+>>>>-]<<<<>[-]<<
+>]<[[>+<-]+++++++[<+++++++>-]<-><.[-]>>[<<+>>-]<<-]>++++[<++++++++>-]<.[-]>>>>>>>]<<<<<<<<>[-]<
[-]<<-]++++++++++.[-]@";
            const string InputString = "25\n";
            const string Expected = "Primes up to: 2 3 5 7 11 13 17 19 23";
            Assert.Equal(Expected, Test(action, Code, optimize, InputString));
        }

        private static string Test(Action<Module> action, string source, bool optimize, string stdin = "")
        {
            Module module = Parser.Parse(source);
            if (optimize)
            {
                module = new Optimizer(Setting.Default).Optimize(module);
            }

            using (var reader = new StringReader(stdin))
            using (var writer = new StringWriter())
            {
                Console.SetIn(reader);
                Console.SetOut(writer);
                action(module);
                Console.Out.Flush();
                return writer.ToString().TrimEnd();
            }
        }

        private const int DefaultBufferSize = 1;
        private static readonly Type[] TestTypes = { typeof(Byte), typeof(Int16), typeof(Int32), typeof(Int64) };

        // (object description, Action<Module> action, bool optimize)
        public static IEnumerable<object[]> ExecutionCase
        {
            get
            {
                foreach (var Type in TestTypes)
                {
                    foreach (var Optimize in new[] { false, true })
                    {
                        yield return new object[]
                        {
                            new { Type, Optimize },
                            GetInterpreterAction(Type),
                            Optimize
                        };

                        foreach (var Unsafe in new[] { false, true })
                        {
                            yield return new object[]
                            {
                                new { Type, Optimize, Unsafe },
                                GetLowLevelInterpreterAction(Type, Unsafe),
                                Optimize
                            };

                            yield return new object[]
                            {
                                new { Type, Optimize, Unsafe },
                                GetILCompilerAction(Setting.Default.WithElementType(Type).WithUnsafeCode(Unsafe)),
                                Optimize
                            };
                        }
                    }
                }
            }
        }

        private static Action<Module> GetInterpreterAction(Type elementType)
        {
            return module => new Interpreter(Setting.Default.WithElementType(elementType).WithBufferSize(1)).Execute(module);
        }

        private static Action<Module> GetLowLevelInterpreterAction(Type elementType, bool unsafeCode)
        {
            return module =>
            {
                Setting setting = Setting.Default.WithElementType(elementType);
                if (unsafeCode)
                {
                    setting = setting.WithUnsafeCode(true);
                }
                else
                {
                    setting = setting.WithBufferSize(1);
                }

                new LowLevelInterpreter(setting).Execute(module.Root.ToLowLevel(setting));
            };
        }

        private static Action<Module> GetILCompilerAction(Setting setting)
        {
            return module =>
            {
                ILCompiler compiler = new ILCompiler(setting);
                Action action = compiler.Compile(module);
                action();
            };
        }
    }
}
