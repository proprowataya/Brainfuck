﻿using System;

namespace Brainfuck.Core
{
    public class Interpreter
    {
        public Setting Setting { get; }

        public Interpreter(Setting setting)
        {
            Setting = setting;
        }

        public void Execute(Program program)
        {
            if (Setting.ElementType == typeof(Int16))
            {
                Execute<Int16, Int16Operator>(program, Setting.BufferSize);
            }
            else if (Setting.ElementType == typeof(Int32))
            {
                Execute<Int32, Int32Operator>(program, Setting.BufferSize);
            }
            else if (Setting.ElementType == typeof(Int64))
            {
                Execute<Int64, Int64Operator>(program, Setting.BufferSize);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type '{Setting.BufferSize}'");
            }
        }

        internal static void Execute<T, TOperator>(Program program, int bufferSize) where TOperator : IOperator<T>
        {
            TOperator op = default(TOperator);
            T[] buffer = new T[bufferSize];
            int ptr = 0;

            for (int i = 0; i < program.Operations.Length; i++)
            {
                Operation operation = program.Operations[i];
                int value = operation.Value;
                ref T current = ref buffer[ptr];

                switch (operation.Opcode)
                {
                    case Opcode.AddPtr:
                        ptr += value;
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
                        current = op.Add(current, value);
                        break;
                    case Opcode.Put:
                        Put(op.ToChar(current));
                        break;
                    case Opcode.Read:
                        current = op.FromInt(Read());
                        break;
                    case Opcode.OpeningBracket:
                        if (op.IsZero(current))
                        {
                            i = value;
                        }
                        break;
                    case Opcode.ClosingBracket:
                        if (op.IsNotZero(current))
                        {
                            i = value;
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
