﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Brainfuck.Core.LowLevel
{
    public unsafe partial class LowLevelInterpreter
    {
        private unsafe void ExecuteUnsafeInt32(ImmutableArray<LowLevelOperation> operations, CancellationToken token)
        {
            Int32[] buffer = new Int32[Setting.BufferSize];
            long step = -1;

            fixed (LowLevelOperation* ops = operations.ToArray())
            fixed (Int32* ptrBase = buffer)
            {
                Int32* ptr = ptrBase;
                step++;
                OnStepStart?.Invoke(new OnStepStartEventArgs((int)(ptr - ptrBase), step, null, new ArrayView<Int32>(buffer)));  // TODO
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < operations.Length; i++)
                {
                    LowLevelOperation op = ops[i];
                    switch (op.Opcode)
                    {
                        case Opcode.AddPtr:
                            {
                                ptr += op.Value;
                                break;
                            }
                        case Opcode.Assign:
                            {
                                ptr[op.Dest] = op.Value;
                                break;
                            }
                        case Opcode.AddAssign:
                            {
                                ptr[op.Dest] += op.Value;
                                break;
                            }
                        case Opcode.MultAddAssign:
                            {
                                ptr[op.Dest] += ptr[op.Src] * op.Value;
                                break;
                            }
                        case Opcode.Put:
                            {
                                Put((char)ptr[op.Src]);
                                break;
                            }
                        case Opcode.Read:
                            {
                                ptr[op.Dest] = Read();
                                break;
                            }
                        case Opcode.BrTrue:
                            {
                                if (ptr[op.Src] != 0)
                                {
                                    i = op.Value;
                                }
                                break;
                            }
                        case Opcode.BrFalse:
                            {
                                if (ptr[op.Src] == 0)
                                {
                                    i = op.Value;
                                }
                                break;
                            }
                        case Opcode.EnsureBuffer:
                            throw new NotImplementedException();
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
        }

        private unsafe void ExecuteUnsafeInt64(ImmutableArray<LowLevelOperation> operations, CancellationToken token)
        {
            Int64[] buffer = new Int64[Setting.BufferSize];
            long step = -1;

            fixed (LowLevelOperation* ops = operations.ToArray())
            fixed (Int64* ptrBase = buffer)
            {
                Int64* ptr = ptrBase;
                step++;
                OnStepStart?.Invoke(new OnStepStartEventArgs((int)(ptr - ptrBase), step, null, new ArrayView<Int64>(buffer)));  // TODO
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < operations.Length; i++)
                {
                    LowLevelOperation op = ops[i];
                    switch (op.Opcode)
                    {
                        case Opcode.AddPtr:
                            {
                                ptr += op.Value;
                                break;
                            }
                        case Opcode.Assign:
                            {
                                ptr[op.Dest] = op.Value;
                                break;
                            }
                        case Opcode.AddAssign:
                            {
                                ptr[op.Dest] += op.Value;
                                break;
                            }
                        case Opcode.MultAddAssign:
                            {
                                ptr[op.Dest] += ptr[op.Src] * op.Value;
                                break;
                            }
                        case Opcode.Put:
                            {
                                Put((char)ptr[op.Src]);
                                break;
                            }
                        case Opcode.Read:
                            {
                                ptr[op.Dest] = Read();
                                break;
                            }
                        case Opcode.BrTrue:
                            {
                                if (ptr[op.Src] != 0)
                                {
                                    i = op.Value;
                                }
                                break;
                            }
                        case Opcode.BrFalse:
                            {
                                if (ptr[op.Src] == 0)
                                {
                                    i = op.Value;
                                }
                                break;
                            }
                        case Opcode.EnsureBuffer:
                            throw new NotImplementedException();
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
        }
    }
}