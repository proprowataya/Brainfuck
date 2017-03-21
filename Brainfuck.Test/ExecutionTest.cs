using System;
using System.IO;
using Xunit;
using Brainfuck.Core;

namespace Brainfuck.Test
{
    public class ExecutionTest
    {
        private const int DefaultBufferSize = 1;
        private static readonly Type[] TestTypes = { typeof(Byte), typeof(Int16), typeof(Int32), typeof(Int64) };

        [Fact]
        public void HelloWorldTest()
        {
            const string HelloWorld = "Hello World!";
            const string Code = ">+++++++++[<++++++++>-]<.>+++++++[<++++>-]<+.+++++++..+++.[-]>++++++++[<++++>-]<.>+++++++++++[<+++++>-]<.>++++++++[<+++>-]<.+++.------.--------.[-]>++++++++[<++++>-]<+.[-]++++++++++.";
            TestAll(Code, HelloWorld);
        }

        [Fact]
        public void HelloWorldWithErrorsTest()
        {
            const string HelloWorld = "Hello World!";
            const string Code = ">++++  + + + ++111111[<1+1+1+1+1+1+1+1+1>1-1]1<1.1>1+1+1+++++[<++++>-]<+.+++++++..+++.[-]>++++++++[<++++>-]<.>+++++++++++[<+++++>-]<.>++++++++[<+++>-]<.+++.------.--------.[-]>++++++++[<++++>-]<+.[-]+++++++++11+.";
            TestAll(Code, HelloWorld);
        }

        [Fact]
        public void EchoTest()
        {
            const string InputString = "This is a test string.\0";
            const string Code = "+[,.]";
            TestAll(Code, InputString, InputString);
        }

        [Fact]
        public void EchoTest2()
        {
            const string InputString = "This is a test string.\0";
            const string Code = ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>+[,.]";
            TestAll(Code, InputString, InputString);
        }

        [Fact]
        public void PrimeTest()
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
            TestAll(Code, Expected, InputString);
        }

        private static void TestAll(string code, string expected, string stdin = "")
        {
            Module[] modules = { Parser.Parse(code), Optimizer.Optimize(Parser.Parse(code)) };

            foreach (var module in modules)
            {
                foreach (var type in TestTypes)
                {
                    Assert.Equal(expected, RunTest(GetInterpreterAction(type), module, stdin));

                    foreach (var unsafeCode in new[] { true, false })
                    {
                        Setting setting = Setting.Default.WithElementType(type).WithUnsafeCode(unsafeCode);
                        Assert.Equal(expected, RunTest(GetILCompilerAction(setting), module, stdin));
                    }
                }
            }
        }

        private static string RunTest(Action<Module> action, Module module, string stdin)
        {
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

        private static Action<Module> GetInterpreterAction(Type elementType)
        {
            return module => new Interpreter(Setting.Default.WithElementType(elementType)).Execute(module);
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
