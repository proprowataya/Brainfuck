using Brainfuck.Core.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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
                PreRoutine(op);
                ExecuteOperations(op.Operations, op.PtrChange);
            }

            public void Visit(IfTrueUnitOperation op)
            {
                PreRoutine(op);
                if (default(TOperator).IsNotZero(buffer[ptr + op.Src.Offset]))
                {
                    ExecuteOperations(op.Operations, op.PtrChange);
                }
            }

            public void Visit(RoopUnitOperation op)
            {
                PreRoutine(op);
                while (default(TOperator).IsNotZero(buffer[ptr + op.Src.Offset]))
                {
                    ExecuteOperations(op.Operations, op.PtrChange);
                    EnsureBufferCapacity(op);
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
                PreRoutine(op);
                ptr += op.Value;
            }

            public void Visit(AssignOperation op)
            {
                PreRoutine(op);
                buffer[ptr + op.Dest.Offset] = default(TOperator).FromInt(op.Value);
            }

            public void Visit(AddAssignOperation op)
            {
                PreRoutine(op);
                ref T dest = ref buffer[ptr + op.Dest.Offset];
                dest = default(TOperator).Add(dest, op.Value);
            }

            public void Visit(MultAddAssignOperation op)
            {
                PreRoutine(op);
                TOperator top = default(TOperator);
                ref T src = ref buffer[ptr + op.Src.Offset];
                ref T dest = ref buffer[ptr + op.Dest.Offset];
                dest = top.Add(dest, top.Mult(src, op.Value));
            }

            public void Visit(PutOperation op)
            {
                PreRoutine(op);
                Put(default(TOperator).ToChar(buffer[ptr + op.Src.Offset]));
            }

            public void Visit(ReadOperation op)
            {
                PreRoutine(op);
                buffer[ptr + op.Dest.Offset] = default(TOperator).FromInt(Read());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void PreRoutine(IOperation op)
            {
                token.ThrowIfCancellationRequested();
                EnsureBufferCapacity(op);
                parent.OnStepStart?.Invoke(new OnStepStartEventArgs(op, new ArrayView<T>(buffer), ptr, step));
                step++;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureBufferCapacity(IOperation op)
            {
                int maxAccessPtr = 0;
                if (op is IReadOperation read)
                {
                    maxAccessPtr = Math.Max(read.Src.Offset, maxAccessPtr);
                }
                if (op is IWriteOperation write)
                {
                    maxAccessPtr = Math.Max(write.Dest.Offset, maxAccessPtr);
                }
                maxAccessPtr += ptr;

                if (maxAccessPtr >= buffer.Length)
                {
                    ExpandBuffer(maxAccessPtr + 1);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ExpandBuffer(int minLength)
            {
                int newSize = Math.Max(buffer.Length + buffer.Length / 2, minLength);
                T[] newBuffer = new T[newSize];
                Array.Copy(buffer, newBuffer, buffer.Length);
                buffer = newBuffer;
            }
        }

        private static int Read() => Console.Read();
        private static void Put(char value) => Console.Write(value);
    }

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
