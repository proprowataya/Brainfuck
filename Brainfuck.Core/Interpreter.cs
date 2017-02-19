using System;

namespace Brainfuck.Core
{
    using BufferElement = Byte;

    public static class Interpreter
    {
        public static void Execute(string source)
        {
            Execute(Parser.Parse(source));
        }

        public static void Execute(Program program)
        {
            Buffer<BufferElement> buffer = new Buffer<BufferElement>();
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
                        buffer[ptr]++;
                        break;
                    case '-':
                        buffer[ptr]--;
                        break;
                    case '.':
                        Put(buffer[ptr]);
                        break;
                    case ',':
                        buffer[ptr] = Read();
                        break;
                    case '[':
                        if (buffer[ptr] == 0)
                        {
                            i = program.OpeningDest[i];
                        }
                        break;
                    case ']':
                        if (buffer[ptr] != 0)
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

        private static BufferElement Read() => (BufferElement)Console.Read();
        private static void Put(BufferElement value) => Console.Write((char)value);
        private static void ManageUnknownChar(char value) => Console.WriteLine($"Warning : Unknown char '{value}'");
    }
}
