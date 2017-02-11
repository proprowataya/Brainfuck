using System;

namespace Brainfuck.Core
{
    using ArrayElement = Byte;

    public static class Interpreter
    {
        public const int ArraySize = 1 << 10;

        public static void Execute(string source)
        {
            Execute(Parser.Parse(source));
        }

        public static void Execute(Program program)
        {
            ArrayElement[] array = new ArrayElement[ArraySize];
            int ptr = 0;

            for (int i = 0; i < program.Source.Length; i++)
            {
                switch (program.Source[i])
                {
                    case '>':
                        ptr++;
                        break;
                    case '<':
                        ptr--;
                        break;
                    case '+':
                        array[ptr]++;
                        break;
                    case '-':
                        array[ptr]--;
                        break;
                    case '.':
                        Put(array[ptr]);
                        break;
                    case ',':
                        array[ptr] = Read();
                        break;
                    case '[':
                        if (array[ptr] == 0)
                        {
                            i = program.OpeningDest[i];
                        }
                        break;
                    case ']':
                        if (array[ptr] != 0)
                        {
                            i = program.ClosingDest[i];
                        }
                        break;
                    default:
                        ManageUnknownChar(program.Source[i]);
                        break;
                }
            }
        }

        private static ArrayElement Read() => (ArrayElement)Console.Read();
        private static void Put(ArrayElement value) => Console.Write((char)value);
        private static void ManageUnknownChar(char value) => Console.WriteLine($"Warning : Unknown char '{value}'");
    }
}
