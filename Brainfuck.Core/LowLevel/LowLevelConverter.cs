using Brainfuck.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Brainfuck.Core.LowLevel
{
    public static class LowLevelConverter
    {
        public static ImmutableArray<LowLevelOperation> ToLowLevel(this IOperation operation, Setting setting)
        {
            var visitor = new Visitor(setting);
            operation.Accept(visitor);
            return visitor.Builder.ToImmutable();
        }

        private class Visitor : IVisitor
        {
            public ImmutableArray<LowLevelOperation>.Builder Builder { get; }
                = ImmutableArray.CreateBuilder<LowLevelOperation>();

            private readonly Setting setting;

            public Visitor(Setting setting)
            {
                this.setting = setting;
            }

            public void Visit(BlockUnitOperation op)
            {
                EmitOperations(op.Operations, op.PtrChange);
            }

            public void Visit(IfTrueUnitOperation op)
            {
                int begin = Builder.Count;
                Builder.Add(default(LowLevelOperation));    // Dummy
                EmitOperations(op.Operations, op.PtrChange);
                int end = Builder.Count - 1;

                // Correct jump address
                Builder[begin] = new LowLevelOperation(Opcode.BrFalse, src: (short)op.Src.Offset, value: (short)end);
            }

            public void Visit(RoopUnitOperation op)
            {
                int begin = Builder.Count;
                Builder.Add(default(LowLevelOperation));    // Dummy
                EmitOperations(op.Operations, op.PtrChange);
                int end = Builder.Count;
                Builder.Add(new LowLevelOperation(Opcode.BrTrue, src: (short)op.Src.Offset, value: (short)begin));

                // Correct jump address
                Builder[begin] = new LowLevelOperation(Opcode.BrFalse, src: (short)op.Src.Offset, value: (short)end);
            }

            public void EmitOperations(ImmutableArray<IOperation> operations, int ptrChange)
            {
                if (setting.UseDynamicBuffer)
                {
                    // Compute max offset difference
                    int maxOffsetDiff = ptrChange;
                    for (int i = 0; i < operations.Length; i++)
                    {
                        if (operations[i] is IReadOperation read)
                        {
                            maxOffsetDiff = Math.Max(maxOffsetDiff, read.Src.Offset);
                        }

                        if (operations[i] is IWriteOperation write)
                        {
                            maxOffsetDiff = Math.Max(maxOffsetDiff, write.Dest.Offset);
                        }
                    }

                    if (maxOffsetDiff > 0)
                    {
                        Builder.Add(new LowLevelOperation(Opcode.EnsureBuffer, value: (short)maxOffsetDiff));
                    }
                }

                for (int i = 0; i < operations.Length; i++)
                {
                    operations[i].Accept(this);
                }

                AddPtr(ptrChange);
            }

            public void Visit(AddPtrOperation op)
            {
                AddPtr(op.Value);
            }

            private void AddPtr(int ptrChange)
            {
                if (ptrChange != 0)
                {
                    Builder.Add(new LowLevelOperation(Opcode.AddPtr, value: (short)ptrChange));
                }
            }

            public void Visit(AssignOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.Assign, dest: (short)op.Dest.Offset, value: (short)op.Value));
            }

            public void Visit(AddAssignOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.AddAssign, dest: (short)op.Dest.Offset, value: (short)op.Value));
            }

            public void Visit(MultAddAssignOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.MultAddAssign, dest: (short)op.Dest.Offset, src: (short)op.Src.Offset, value: (short)op.Value));
            }

            public void Visit(PutOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.Put, src: (short)op.Src.Offset));
            }

            public void Visit(ReadOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.Read, dest: (short)op.Dest.Offset));
            }
        }
    }
}
