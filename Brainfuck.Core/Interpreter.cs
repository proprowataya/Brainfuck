using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private void Execute<T, TOperator>(Program program, CancellationToken token) where TOperator : IOperator<T>
        {
            T[] buffer = new T[Setting.BufferSize];
            int ptr = 0;
            int step = 0;

            ExecuteCore(program.Operations);

            void ExecuteCore(ImmutableArray<IOperation> operations)
            {
                TOperator top = default(TOperator);

                for (int i = 0; i < operations.Length; i++, step++)
                {
                    token.ThrowIfCancellationRequested();
                    OnStepStart?.Invoke(new OnStepStartEventArgs(step, i, ptr, operations[i], new ArrayView<T>(buffer)));

                    EnsureIndex(ptr + operations[i].MaxAccessOffset);

                    switch (operations[i])
                    {
                        case AddPtr op:
                            {
                                ptr += op.Value;
                            }
                            break;
                        case AddValue op:
                            {
                                ref T dest = ref buffer[ptr + op.Dest.Offset];
                                dest = top.Add(dest, op.Value);
                            }
                            break;
                        case MultAdd op:
                            {
                                ref T src = ref buffer[ptr + op.Src.Offset];
                                ref T dest = ref buffer[ptr + op.Dest.Offset];
                                dest = top.Add(dest, top.Mult(src, op.Value));
                            }
                            break;
                        case Assign op:
                            {
                                buffer[ptr + op.Dest.Offset] = top.FromInt(op.Value);
                            }
                            break;
                        case Put op:
                            {
                                Put(top.ToChar(buffer[ptr + op.Src.Offset]));
                            }
                            break;
                        case Read op:
                            {
                                buffer[ptr + op.Dest.Offset] = top.FromInt(Read());
                            }
                            break;
                        case Roop op:
                            {
                                while (top.IsNotZero(buffer[ptr]))
                                {
                                    ExecuteCore(op.Operations);
                                }
                            }
                            break;
                        case IfTrue op:
                            {
                                if (top.IsNotZero(buffer[ptr + op.Condition.Offset]))
                                {
                                    ExecuteCore(op.Operations);
                                }
                            }
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            void EnsureIndex(int index)
            {
                if (index >= buffer.Length)
                {
                    int newSize = Math.Max(buffer.Length * 2, index + 1);
                    T[] newBuffer = new T[newSize];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    buffer = newBuffer;
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
        public IOperation Operation { get; }
        public IReadOnlyList<object> Buffer { get; }

        public OnStepStartEventArgs(int step, int index, int pointer, IOperation operation, IReadOnlyList<object> buffer)
        {
            Step = step;
            Index = index;
            Pointer = pointer;
            Operation = operation;
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
        T Add(T a, T b);
        T Mult(T a, int b);
        bool IsZero(T value);
        bool IsNotZero(T value);
        T FromInt(int value);
        char ToChar(T value);
    }

    internal struct UInt8Operator : IOperator<UInt8>
    {
        public UInt8 Add(UInt8 a, int b) => new UInt8((byte)(a.Value + b));
        public UInt8 Add(UInt8 a, UInt8 b) => (UInt8)(a + b);
        public UInt8 Mult(UInt8 a, int b) => (UInt8)(a.Value * b);
        public bool IsZero(UInt8 value) => value == 0;
        public bool IsNotZero(UInt8 value) => value != 0;
        public UInt8 FromInt(int value) => (UInt8)value;
        public char ToChar(UInt8 value) => (char)value;
    }

    internal struct Int16Operator : IOperator<Int16>
    {
        public Int16 Add(Int16 a, int b) => (Int16)(a + b);
        public Int16 Add(Int16 a, Int16 b) => (Int16)(a + b);
        public Int16 Mult(Int16 a, int b) => (Int16)(a * b);
        public bool IsZero(Int16 value) => value == 0;
        public bool IsNotZero(Int16 value) => value != 0;
        public Int16 FromInt(int value) => (Int16)value;
        public char ToChar(Int16 value) => (char)value;
    }

    internal struct Int32Operator : IOperator<Int32>
    {
        public Int32 Add(Int32 a, int b) => (Int32)(a + b);
        public Int32 Mult(Int32 a, int b) => (Int32)(a * b);
        public bool IsZero(Int32 value) => value == 0;
        public bool IsNotZero(Int32 value) => value != 0;
        public Int32 FromInt(int value) => (Int32)value;
        public char ToChar(Int32 value) => (char)value;
    }

    internal struct Int64Operator : IOperator<Int64>
    {
        public Int64 Add(Int64 a, int b) => (Int64)(a + b);
        public Int64 Add(Int64 a, Int64 b) => (Int64)(a + b);
        public Int64 Mult(Int64 a, int b) => (Int64)(a * b);
        public bool IsZero(Int64 value) => value == 0;
        public bool IsNotZero(Int64 value) => value != 0;
        public Int64 FromInt(int value) => (Int64)value;
        public char ToChar(Int64 value) => (char)value;
    }

    #endregion
}
