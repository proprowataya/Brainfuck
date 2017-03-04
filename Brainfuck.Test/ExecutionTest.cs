using System;
using System.IO;
using Xunit;
using Brainfuck.Core;

namespace Brainfuck.Test
{
    public class ExecutionTest
    {
        private static readonly Type[] TestTypes = { typeof(Int16), typeof(Int32), typeof(Int64) };

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

        private static void TestAll(string code, string expected)
        {
            Program[] programs = { Parser.Parse(code), Optimizer.Optimize(Parser.Parse(code)) };

            foreach (var program in programs)
            {
                Assert.Equal(expected, RunTest(GetInterpreterAction(), program));

                foreach (var type in TestTypes)
                {
                    Assert.Equal(expected, RunTest(GetCompilerAction(type), program));
                }
            }
        }

        private static string RunTest(Action<Program> action, Program program)
        {
            using (var writer = new StringWriter())
            {
                Console.SetOut(writer);
                action(program);
                Console.Out.Flush();
                return writer.ToString().TrimEnd();
            }
        }

        private static Action<Program> GetInterpreterAction()
        {
            return program => Interpreter.Execute(program);
        }

        private static Action<Program> GetCompilerAction(Type elementType)
        {
            return program =>
            {
                Compiler compiler = new Compiler(CompilerSetting.Default.WithElementType(elementType));
                Action action = compiler.Compile(program);
                action();
            };
        }
    }
}
