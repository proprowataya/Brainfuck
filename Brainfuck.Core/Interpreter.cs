using System;

namespace Brainfuck.Core
{
    using BufferElement = Int32;

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
                switch (program.Operations[i].Opcode)
                {
                    case Opcode.AddPtr:
                        ptr += program.Operations[i].Value;
                        break;
                    case Opcode.AddValue:
                        buffer[ptr] += program.Operations[i].Value;
                        break;
                    case Opcode.Put:
                        Put(buffer[ptr]);
                        break;
                    case Opcode.Read:
                        buffer[ptr] = Read();
                        break;
                    case Opcode.OpeningBracket:
                        if (buffer[ptr] == 0)
                        {
                            i = program.Operations[i].Value;
                        }
                        break;
                    case Opcode.ClosingBracket:
                        if (buffer[ptr] != 0)
                        {
                            i = program.Operations[i].Value;
                        }
                        break;
                    case Opcode.Unknown:
                    default:
                        // Do nothing
                        break;
                }
            }
        }

        private static BufferElement Read() => (BufferElement)Console.Read();
        private static void Put(BufferElement value) => Console.Write((char)value);
    }
}
