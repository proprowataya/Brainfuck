using System;

namespace Brainfuck.Core
{
    public static class Interpreter
    {
        public static void Execute(Program program)
            => Execute(program, Defaults.ElementType, Defaults.BufferSize);

        public static void Execute(Program program, Type elementType)
            => Execute(program, elementType, Defaults.BufferSize);

        public static void Execute(Program program, Type elementType, int bufferSize)
        {
            if (elementType == typeof(Int16))
            {
                Execute<Int16, Int16Operator>(program, bufferSize);
            }
            else if (elementType == typeof(Int32))
            {
                Execute<Int32, Int32Operator>(program, bufferSize);
            }
            else if (elementType == typeof(Int64))
            {
                Execute<Int64, Int64Operator>(program, bufferSize);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type '{elementType}'");
            }
        }

        internal static void Execute<T, TOperator>(Program program, int bufferSize) where TOperator : IOperator<T>
        {
            TOperator op = default(TOperator);
            T[] buffer = new T[bufferSize];
            int ptr = 0;

            for (int i = 0; i < program.Operations.Length; i++)
            {
                switch (program.Operations[i].Opcode)
                {
                    case Opcode.AddPtr:
                        ptr += program.Operations[i].Value;
                        if (ptr >= buffer.Length)
                        {
                            // Expand buffer
                            int newSize = Math.Max(buffer.Length * 2, ptr + 1);
                            T[] newBuffer = new T[newSize];
                            Array.Copy(buffer, newBuffer, buffer.Length);
                            buffer = newBuffer;
                        }
                        break;
                    case Opcode.AddValue:
                        buffer[ptr] = op.Add(buffer[ptr], program.Operations[i].Value);
                        break;
                    case Opcode.Put:
                        Put(op.ToChar(buffer[ptr]));
                        break;
                    case Opcode.Read:
                        buffer[ptr] = op.FromInt(Read());
                        break;
                    case Opcode.OpeningBracket:
                        if (op.IsZero(buffer[ptr]))
                        {
                            i = program.Operations[i].Value;
                        }
                        break;
                    case Opcode.ClosingBracket:
                        if (op.IsNotZero(buffer[ptr]))
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

        private static int Read() => Console.Read();
        private static void Put(char value) => Console.Write(value);
    }

    internal interface IOperator<T>
    {
        T Add(T a, int b);
        bool IsZero(T value);
        bool IsNotZero(T value);
        T FromInt(int value);
        char ToChar(T value);
    }

    internal struct Int16Operator : IOperator<Int16>
    {
        public Int16 Add(Int16 a, int b) => (Int16)(a + b);
        public bool IsZero(Int16 value) => value == 0;
        public bool IsNotZero(Int16 value) => value != 0;
        public Int16 FromInt(int value) => (Int16)value;
        public char ToChar(Int16 value) => (char)value;
    }

    internal struct Int32Operator : IOperator<Int32>
    {
        public Int32 Add(Int32 a, int b) => (Int32)(a + b);
        public bool IsZero(Int32 value) => value == 0;
        public bool IsNotZero(Int32 value) => value != 0;
        public Int32 FromInt(int value) => (Int32)value;
        public char ToChar(Int32 value) => (char)value;
    }

    internal struct Int64Operator : IOperator<Int64>
    {
        public Int64 Add(Int64 a, int b) => (Int64)(a + b);
        public bool IsZero(Int64 value) => value == 0;
        public bool IsNotZero(Int64 value) => value != 0;
        public Int64 FromInt(int value) => (Int64)value;
        public char ToChar(Int64 value) => (char)value;
    }
}
