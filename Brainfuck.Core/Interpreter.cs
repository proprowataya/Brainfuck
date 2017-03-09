using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Brainfuck.Core
{
    public class Interpreter
    {
        public Setting Setting { get; }
        public event OnStepStartEventHandler OnStepStart;

        public Interpreter(Setting setting)
        {
            Setting = setting;
        }

        public void Execute(Program program, CancellationToken token = default(CancellationToken))
        {
            if (Setting.ElementType == typeof(Byte))
            {
                Execute<UInt8, UInt8Operator>(program, token);
            }
            else if (Setting.ElementType == typeof(Int16))
            {
                Execute<Int16, Int16Operator>(program, token);
            }
            else if (Setting.ElementType == typeof(Int32))
            {
                Execute<Int32, Int32Operator>(program, token);
            }
            else if (Setting.ElementType == typeof(Int64))
            {
                Execute<Int64, Int64Operator>(program, token);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type '{Setting.BufferSize}'");
            }
        }

        internal void Execute<T, TOperator>(Program program, CancellationToken token) where TOperator : IOperator<T>
        {
            TOperator op = default(TOperator);
            T[] buffer = new T[Setting.BufferSize];
            int ptr = 0;
            int step = 0;

            for (int i = 0; i < program.Operations.Length; i++, step++)
            {
                token.ThrowIfCancellationRequested();
                OnStepStart?.Invoke(new OnStepStartEventArgs(step, i, ptr, new ArrayView<T>(buffer)));

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

    #region Event Handler

    public delegate void OnStepStartEventHandler(OnStepStartEventArgs args);

    public sealed class OnStepStartEventArgs
    {
        public int Step { get; }
        public int Index { get; }
        public int Pointer { get; }
        public IReadOnlyList<object> Buffer { get; }

        public OnStepStartEventArgs(int step, int index, int pointer, IReadOnlyList<object> buffer)
        {
            Step = step;
            Index = index;
            Pointer = pointer;
            Buffer = buffer;
        }
    }

    internal struct ArrayView<T> : IReadOnlyList<object>
    {
        private readonly T[] _array;

        public ArrayView(T[] array)
        {
            _array = array;
        }

        public int Length => _array.Length;
        public object this[int index] => _array[index];
        int IReadOnlyCollection<object>.Count => _array.Length;

        public IEnumerator<object> GetEnumerator()
        {
            for (int i = 0; i < _array.Length; i++)
            {
                yield return _array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    #endregion

    #region Operator

    internal interface IOperator<T>
    {
        T Add(T a, int b);
        bool IsZero(T value);
        bool IsNotZero(T value);
        T FromInt(int value);
        char ToChar(T value);
    }

    internal struct UInt8Operator : IOperator<UInt8>
    {
        public UInt8 Add(UInt8 a, int b) => new UInt8((byte)(a.Value + b));
        public bool IsZero(UInt8 value) => value == 0;
        public bool IsNotZero(UInt8 value) => value != 0;
        public UInt8 FromInt(int value) => (UInt8)value;
        public char ToChar(UInt8 value) => (char)value;
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

    #endregion
}
