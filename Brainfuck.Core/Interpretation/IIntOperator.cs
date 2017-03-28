using System;

namespace Brainfuck.Core.Interpretation
{
    internal interface IIntOperator<T>
    {
        T Add(T a, int b);
        T Add(T a, T b);
        T Mult(T a, int b);
        bool IsZero(T value);
        bool IsNotZero(T value);
        T FromInt(int value);
        char ToChar(T value);
    }

    internal struct ByteOperator : IIntOperator<Byte>
    {
        public Byte Add(Byte a, int b) => (Byte)(a + b);
        public Byte Add(Byte a, Byte b) => (Byte)(a + b);
        public Byte Mult(Byte a, int b) => (Byte)(a * b);
        public bool IsZero(Byte value) => value == 0;
        public bool IsNotZero(Byte value) => value != 0;
        public Byte FromInt(int value) => (Byte)value;
        public char ToChar(Byte value) => (char)value;
    }

    internal struct Int16Operator : IIntOperator<Int16>
    {
        public Int16 Add(Int16 a, int b) => (Int16)(a + b);
        public Int16 Add(Int16 a, Int16 b) => (Int16)(a + b);
        public Int16 Mult(Int16 a, int b) => (Int16)(a * b);
        public bool IsZero(Int16 value) => value == 0;
        public bool IsNotZero(Int16 value) => value != 0;
        public Int16 FromInt(int value) => (Int16)value;
        public char ToChar(Int16 value) => (char)value;
    }

    internal struct Int32Operator : IIntOperator<Int32>
    {
        public Int32 Add(Int32 a, int b) => (Int32)(a + b);
        public Int32 Mult(Int32 a, int b) => (Int32)(a * b);
        public bool IsZero(Int32 value) => value == 0;
        public bool IsNotZero(Int32 value) => value != 0;
        public Int32 FromInt(int value) => (Int32)value;
        public char ToChar(Int32 value) => (char)value;
    }

    internal struct Int64Operator : IIntOperator<Int64>
    {
        public Int64 Add(Int64 a, int b) => (Int64)(a + b);
        public Int64 Add(Int64 a, Int64 b) => (Int64)(a + b);
        public Int64 Mult(Int64 a, int b) => (Int64)(a * b);
        public bool IsZero(Int64 value) => value == 0;
        public bool IsNotZero(Int64 value) => value != 0;
        public Int64 FromInt(int value) => (Int64)value;
        public char ToChar(Int64 value) => (char)value;
    }
}
