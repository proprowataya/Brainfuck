using Brainfuck.Core.LowLevel;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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

        public void Execute(LowLevelModule module, CancellationToken token = default(CancellationToken))
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
                if (Setting.UnsafeCode)
                {
                    ExecuteUnsafeInt32(module, token);
                }
                else
                {
                    Execute<Int32, Int32Operator>(module, token);
                }
            }
            else if (Setting.ElementType == typeof(Int64))
            {
                if (Setting.UnsafeCode)
                {
                    ExecuteUnsafeInt64(module, token);
                }
                else
                {
                    Execute<Int64, Int64Operator>(module, token);
                }
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type '{Setting.BufferSize}'");
            }
        }

        private void Execute<T, TOperator>(LowLevelModule module, CancellationToken token) where TOperator : IIntOperator<T>
        {
            TOperator top = default(TOperator);
            T[] buffer = new T[Setting.BufferSize];
            T[] registers = new T[module.NumRegisters];
            int ptr = 0;
            long step = 0;

            for (int i = 0; ; i++, step++)
            {
                OnStepStart?.Invoke(new OnStepStartEventArgs(buffer, ptr, i, step));
                token.ThrowIfCancellationRequested();

                LowLevelOperation op = module.Operations[i];
                switch (op.Opcode)
                {
                    case Opcode.AddPtr:
                        {
                            ptr += op.Value;
                            break;
                        }
                    case Opcode.Assign:
                        {
                            GetRef(op.Dest, buffer, ptr, registers) = top.FromInt(op.Value);
                            break;
                        }
                    case Opcode.AddAssign:
                        {
                            ref T src = ref GetRef(op.Dest, buffer, ptr, registers);
                            src = top.Add(src, op.Value);
                            break;
                        }
                    case Opcode.MultAddAssign:
                        {
                            ref T src = ref GetRef(op.Src, buffer, ptr, registers);
                            ref T dest = ref GetRef(op.Dest, buffer, ptr, registers);
                            dest = top.Add(dest, top.Mult(src, op.Value));
                            break;
                        }
                    case Opcode.Put:
                        {
                            Put(top.ToChar(GetRef(op.Src, buffer, ptr, registers)));
                            break;
                        }
                    case Opcode.Read:
                        {
                            GetRef(op.Dest, buffer, ptr, registers) = top.FromInt(Read());
                            break;
                        }
                    case Opcode.BrTrue:
                        {
                            if (top.IsNotZero(GetRef(op.Src, buffer, ptr, registers)))
                            {
                                i = op.Value;
                            }
                            break;
                        }
                    case Opcode.BrFalse:
                        {
                            if (top.IsZero(GetRef(op.Src, buffer, ptr, registers)))
                            {
                                i = op.Value;
                            }
                            break;
                        }
                    case Opcode.Return:
                        {
                            return;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T GetRef<T>(Variable var, T[] buffer, int ptr, T[] registers)
        {
            if (var.IsRegister)
            {
                return ref registers[var.RegisterNo];
            }
            else
            {
                return ref buffer[ptr + var.Offset];
            }
        }

        private static int Read() => Console.Read();
        private static void Put(char value) => Console.Write(value);
    }
}
