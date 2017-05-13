using Brainfuck.Core.LowLevel;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Brainfuck.Core.Interpretation
{
    public unsafe partial class Interpreter
    {
        private unsafe void ExecuteUnsafeInt32(LowLevelModule module, CancellationToken token)
        {
            Int32[] buffer = new Int32[Setting.BufferSize];
            Int32[] registers = new Int32[module.NumRegisters];
            long step = 0;

            fixed (LowLevelOperation* opBase = module.Operations.ToArray())
            fixed (Int32* ptrBase = buffer)
            fixed (Int32* reg = registers)
            {
                Int32* ptr = ptrBase;
                LowLevelOperation* op = opBase;

                for (; ; op++, step++)
                {
                    OnStepStart?.Invoke(new OnStepStartEventArgs(buffer, (int)(ptr - ptrBase), (int)(op - opBase), step));
                    token.ThrowIfCancellationRequested();

                    switch (op->Opcode)
                    {
                        case Opcode.AddPtr:
                            {
                                ptr += op->Value;
                                break;
                            }
                        case Opcode.Assign:
                            {
                                GetRef(op->Dest, ptr, reg) = op->Value;
                                break;
                            }
                        case Opcode.AddAssign:
                            {
                                GetRef(op->Dest, ptr, reg) += op->Value;
                                break;
                            }
                        case Opcode.MultAddAssign:
                            {
                                GetRef(op->Dest, ptr, reg) += GetRef(op->Src, ptr, reg) * op->Value;
                                break;
                            }
                        case Opcode.Put:
                            {
                                Put((char)GetRef(op->Src, ptr, reg));
                                break;
                            }
                        case Opcode.Read:
                            {
                                GetRef(op->Dest, ptr, reg) = Read();
                                break;
                            }
                        case Opcode.BrTrue:
                            {
                                if (GetRef(op->Src, ptr, reg) != 0)
                                {
                                    op = opBase + op->Value;
                                }
                                break;
                            }
                        case Opcode.BrFalse:
                            {
                                if (GetRef(op->Src, ptr, reg) == 0)
                                {
                                    op = opBase + op->Value;
                                }
                                break;
                            }
                        case Opcode.Return:
                            {
                                return;
                            }
                        case Opcode.EnsureBuffer:
                            throw new NotImplementedException();
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
        }

        private unsafe ref Int32 GetRef(Variable var, Int32* ptr, Int32* reg)
        {
            if (var.IsRegister)
            {
                return ref reg[var.RegisterNo];
            }
            else
            {
                return ref ptr[var.Offset];
            }
        }

        private unsafe void ExecuteUnsafeInt64(LowLevelModule module, CancellationToken token)
        {
            Int64[] buffer = new Int64[Setting.BufferSize];
            Int64[] registers = new Int64[module.NumRegisters];
            long step = 0;

            fixed (LowLevelOperation* opBase = module.Operations.ToArray())
            fixed (Int64* ptrBase = buffer)
            fixed (Int64* reg = registers)
            {
                Int64* ptr = ptrBase;
                LowLevelOperation* op = opBase;

                for (; ; op++, step++)
                {
                    OnStepStart?.Invoke(new OnStepStartEventArgs(buffer, (int)(ptr - ptrBase), (int)(op - opBase), step));
                    token.ThrowIfCancellationRequested();

                    switch (op->Opcode)
                    {
                        case Opcode.AddPtr:
                            {
                                ptr += op->Value;
                                break;
                            }
                        case Opcode.Assign:
                            {
                                GetRef(op->Dest, ptr, reg) = op->Value;
                                break;
                            }
                        case Opcode.AddAssign:
                            {
                                GetRef(op->Dest, ptr, reg) += op->Value;
                                break;
                            }
                        case Opcode.MultAddAssign:
                            {
                                GetRef(op->Dest, ptr, reg) += GetRef(op->Src, ptr, reg) * op->Value;
                                break;
                            }
                        case Opcode.Put:
                            {
                                Put((char)GetRef(op->Src, ptr, reg));
                                break;
                            }
                        case Opcode.Read:
                            {
                                GetRef(op->Dest, ptr, reg) = Read();
                                break;
                            }
                        case Opcode.BrTrue:
                            {
                                if (GetRef(op->Src, ptr, reg) != 0)
                                {
                                    op = opBase + op->Value;
                                }
                                break;
                            }
                        case Opcode.BrFalse:
                            {
                                if (GetRef(op->Src, ptr, reg) == 0)
                                {
                                    op = opBase + op->Value;
                                }
                                break;
                            }
                        case Opcode.Return:
                            {
                                return;
                            }
                        case Opcode.EnsureBuffer:
                            throw new NotImplementedException();
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
        }

        private unsafe ref Int64 GetRef(Variable var, Int64* ptr, Int64* reg)
        {
            if (var.IsRegister)
            {
                return ref reg[var.RegisterNo];
            }
            else
            {
                return ref ptr[var.Offset];
            }
        }
    }
}
