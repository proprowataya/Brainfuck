using System;
using System.IO;
using Xunit;
using Brainfuck.Core;
using System.Collections.Generic;

namespace Brainfuck.Test
{
    public class ExecutionTest
    {
        [Theory, MemberData(nameof(ExecutionCase))]
        public void HelloWorldTest(object description, Action<Program> action, bool optimize)
        {
            const string HelloWorld = "Hello World!";
            const string Code = ">+++++++++[<++++++++>-]<.>+++++++[<++++>-]<+.+++++++..+++.[-]>++++++++[<++++>-]<.>+++++++++++[<+++++>-]<.>++++++++[<+++>-]<.+++.------.--------.[-]>++++++++[<++++>-]<+.[-]++++++++++.";
            Assert.Equal(HelloWorld, Test(action, Code, optimize));
        }

        [Theory, MemberData(nameof(ExecutionCase))]
        public void HelloWorldWithErrorsTest(object description, Action<Program> action, bool optimize)
        {
            const string HelloWorld = "Hello World!";
            const string Code = ">++++  + + + ++111111[<1+1+1+1+1+1+1+1+1>1-1]1<1.1>1+1+1+++++[<++++>-]<+.+++++++..+++.[-]>++++++++[<++++>-]<.>+++++++++++[<+++++>-]<.>++++++++[<+++>-]<.+++.------.--------.[-]>++++++++[<++++>-]<+.[-]+++++++++11+.";
            Assert.Equal(HelloWorld, Test(action, Code, optimize));
        }

        [Theory, MemberData(nameof(ExecutionCase))]
        public void EchoTest(object description, Action<Program> action, bool optimize)
        {
            const string InputString = "This is a test string.\0";
            const string Code = "+[,.]";
            Assert.Equal(InputString, Test(action, Code, optimize, InputString));
        }

        [Theory, MemberData(nameof(ExecutionCase))]
        public void EchoTest2(object description, Action<Program> action, bool optimize)
        {
            const string InputString = "This is a test string.\0";
            const string Code = ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>+[,.]";
            Assert.Equal(InputString, Test(action, Code, optimize, InputString));
        }

        [Theory, MemberData(nameof(ExecutionCase))]
        public void PrimeTest(object description, Action<Program> action, bool optimize)
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

        private static string Test(Action<Program> action, string source, bool optimize, string stdin = "")
        {
            Program program = Parser.Parse(source);
            if (optimize)
            {
                program = program.Optimize();
            }

            using (var reader = new StringReader(stdin))
            using (var writer = new StringWriter())
            {
                Console.SetIn(reader);
                Console.SetOut(writer);
                action(program);
                Console.Out.Flush();
                return writer.ToString().TrimEnd();
            }
        }

        private const int DefaultBufferSize = 1;
        private static readonly Type[] TestTypes = { typeof(Byte), typeof(Int16), typeof(Int32), typeof(Int64) };

        // (object description, Action<Program> action, bool optimize)
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
                                GetILCompilerAction(Setting.Default.WithElementType(Type).WithUnsafeCode(Unsafe)),
                                Optimize
                            };
                        }
                    }
                }
            }
        }

        private static Action<Program> GetInterpreterAction(Type elementType)
        {
            return program => new Interpreter(Setting.Default.WithElementType(elementType)).Execute(program);
        }

        private static Action<Program> GetCompilerAction(Setting setting)
        {
            return program =>
            {
                Compiler compiler = new Compiler(setting);
                Action action = compiler.Compile(program);
                action();
            };
        }

        private static Action<Program> GetILCompilerAction(Setting setting)
        {
            return program =>
            {
                ILCompiler compiler = new ILCompiler(setting);
                Action action = compiler.Compile(program);
                action();
            };
        }
    }
}
