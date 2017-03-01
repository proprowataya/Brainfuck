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

        private static void TestAll(string code, string expected)
        {
            Assert.Equal(expected, RunTest(GetInterpreterAction(), code));

            foreach (var type in TestTypes)
            {
                Assert.Equal(expected, RunTest(GetCompilerAction(type), code));
            }
        }

        private static string RunTest(Action<string> action, string code)
        {
            using (var writer = new StringWriter())
            {
                Console.SetOut(writer);
                action(code);
                Console.Out.Flush();
                return writer.ToString().TrimEnd();
            }
        }

        private static Action<string> GetInterpreterAction()
        {
            return code => Interpreter.Execute(code);
        }

        private static Action<string> GetCompilerAction(Type elementType)
        {
            return code =>
            {
                new Compiler(CompilerSetting.Default.WithElementType(elementType)).Compile(Parser.Parse(code));
            };
        }
    }
}
