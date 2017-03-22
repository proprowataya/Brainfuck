using Brainfuck.Core.Syntax;
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

        public void Execute(Module module, CancellationToken token = default(CancellationToken))
        {
            if (Setting.ElementType == typeof(Byte))
            {
                Execute<Byte, ByteOperator>(module, token);
            }
            else if (Setting.ElementType == typeof(Int16))
            {
                Execute<Int16, Int16Operator>(module, token);
            }
            else if (Setting.ElementType == typeof(Int32))
            {
                Execute<Int32, Int32Operator>(module, token);
            }
            else if (Setting.ElementType == typeof(Int64))
            {
                Execute<Int64, Int64Operator>(module, token);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type '{Setting.BufferSize}'");
            }
        }

        private void Execute<T, TOperator>(Module module, CancellationToken token) where TOperator : IIntOperator<T>
        {
            var visitor = new InterpreterImplement<T, TOperator>(this, module, token);
            module.Root.Accept(visitor);
        }

        private class InterpreterImplement<T, TOperator> : IVisitor where TOperator : IIntOperator<T>
        {
            private readonly Interpreter parent;
            private readonly Module module;
            private readonly CancellationToken token;
            private T[] buffer;
            private int ptr;
            private long step;

            public InterpreterImplement(Interpreter parent, Module module, CancellationToken token)
            {
                this.parent = parent;
                this.module = module;
                this.token = token;
                this.buffer = new T[parent.Setting.BufferSize];
                this.ptr = 0;
                this.step = 0;
            }

            public (T[] buffer, int ptr, long step) GetState() => (buffer, ptr, step);

            public void Visit(BlockUnitOperation op)
            {
                ExecuteOperations(op.Operations, op.PtrChange);
            }

            public void Visit(IfTrueUnitOperation op)
            {
                if (default(TOperator).IsNotZero(buffer[ptr + op.Src.Offset]))
                {
                    ExecuteOperations(op.Operations, op.PtrChange);
                }
            }

            public void Visit(RoopUnitOperation op)
            {
                while (default(TOperator).IsNotZero(buffer[ptr + op.Src.Offset]))
                {
                    ExecuteOperations(op.Operations, op.PtrChange);
                }
            }

            private void ExecuteOperations(ImmutableArray<IOperation> operations, int ptrChange)
            {
                for (int i = 0; i < operations.Length; i++)
                {
                    operations[i].Accept(this);
                }

                ptr += ptrChange;
            }

            public void Visit(AddPtrOperation op)
            {
                ptr += op.Value;
            }

            public void Visit(AssignOperation op)
            {
                buffer[ptr + op.Dest.Offset] = default(TOperator).FromInt(op.Value);
            }

            public void Visit(AddAssignOperation op)
            {
                ref T dest = ref buffer[ptr + op.Dest.Offset];
                dest = default(TOperator).Add(dest, op.Value);
            }

            public void Visit(MultAddAssignOperation op)
            {
                TOperator top = default(TOperator);
                ref T src = ref buffer[ptr + op.Src.Offset];
                ref T dest = ref buffer[ptr + op.Dest.Offset];
                dest = top.Add(dest, top.Mult(src, op.Value));
            }

            public void Visit(PutOperation op)
            {
                Put(default(TOperator).ToChar(buffer[ptr + op.Src.Offset]));
            }

            public void Visit(ReadOperation op)
            {
                buffer[ptr + op.Dest.Offset] = default(TOperator).FromInt(Read());
            }
        }

        private static int Read() => Console.Read();
        private static void Put(char value) => Console.Write(value);
    }

    #region Event Handler

    public delegate void OnStepStartEventHandler(OnStepStartEventArgs args);

    public sealed class OnStepStartEventArgs
    {
        public int Pointer { get; }
        public long Step { get; }
        public IOperation Operation { get; }
        public IReadOnlyList<object> Buffer { get; }

        public OnStepStartEventArgs(int pointer, long step, IOperation operation, IReadOnlyList<object> buffer)
        {
            Pointer = pointer;
            Step = step;
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

    #endregion
}
