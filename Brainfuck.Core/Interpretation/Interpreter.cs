using Brainfuck.Core.LowLevel;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace Brainfuck.Core.Interpretation
{
    public partial class Interpreter
    {
        public Setting Setting { get; }
        public event OnStepStartEventHandler OnStepStart;

        public Interpreter(Setting setting)
        {
            Setting = setting;
        }

        public void Execute(ImmutableArray<LowLevelOperation> operations, CancellationToken token = default(CancellationToken))
        {
            if (Setting.ElementType == typeof(Byte))
            {
                Execute<Byte, ByteOperator>(operations, token);
            }
            else if (Setting.ElementType == typeof(Int16))
            {
                Execute<Int16, Int16Operator>(operations, token);
            }
            else if (Setting.ElementType == typeof(Int32))
            {
                if (Setting.UnsafeCode)
                {
                    ExecuteUnsafeInt32(operations, token);
                }
                else
                {
                    Execute<Int32, Int32Operator>(operations, token);
                }
            }
            else if (Setting.ElementType == typeof(Int64))
            {
                if (Setting.UnsafeCode)
                {
                    ExecuteUnsafeInt64(operations, token);
                }
                else
                {
                    Execute<Int64, Int64Operator>(operations, token);
                }
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type '{Setting.BufferSize}'");
            }
        }

        private void Execute<T, TOperator>(ImmutableArray<LowLevelOperation> operations, CancellationToken token) where TOperator : IIntOperator<T>
        {
            TOperator top = default(TOperator);
            T[] buffer = new T[Setting.BufferSize];
            int ptr = 0;
            long step = 0;

            for (int i = 0; i < operations.Length; i++, step++)
            {
                OnStepStart?.Invoke(new OnStepStartEventArgs(buffer, ptr, i, step));
                token.ThrowIfCancellationRequested();

                LowLevelOperation op = operations[i];
                switch (op.Opcode)
                {
                    case Opcode.AddPtr:
                        {
                            ptr += op.Value;
                            break;
                        }
                    case Opcode.Assign:
                        {
                            buffer[ptr + op.Dest] = top.FromInt(op.Value);
                            break;
                        }
                    case Opcode.AddAssign:
                        {
                            ref T src = ref buffer[ptr + op.Dest];
                            src = top.Add(src, op.Value);
                            break;
                        }
                    case Opcode.MultAddAssign:
                        {
                            ref T src = ref buffer[ptr + op.Src];
                            ref T dest = ref buffer[ptr + op.Dest];
                            dest = top.Add(dest, top.Mult(src, op.Value));
                            break;
                        }
                    case Opcode.Put:
                        {
                            Put(top.ToChar(buffer[ptr + op.Src]));
                            break;
                        }
                    case Opcode.Read:
                        {
                            buffer[ptr + op.Dest] = top.FromInt(Read());
                            break;
                        }
                    case Opcode.BrTrue:
                        {
                            if (top.IsNotZero(buffer[ptr + op.Src]))
                            {
                                i = op.Value;
                            }
                            break;
                        }
                    case Opcode.BrFalse:
                        {
                            if (top.IsZero(buffer[ptr + op.Src]))
                            {
                                i = op.Value;
                            }
                            break;
                        }
                    case Opcode.EnsureBuffer:
                        {
                            int maxIndex = ptr + op.Value;
                            if (maxIndex >= buffer.Length)
                            {
                                T[] newBuffer = new T[Math.Max(maxIndex + 1, buffer.Length + buffer.Length / 2)];
                                Array.Copy(buffer, newBuffer, buffer.Length);
                                buffer = newBuffer;
                            }
                            break;
                        }
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private static int Read() => Console.Read();
        private static void Put(char value) => Console.Write(value);
    }
}
