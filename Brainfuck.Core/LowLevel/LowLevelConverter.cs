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
                Builder[begin] = new LowLevelOperation(Opcode.BrFalse, src: op.Src.Offset, value: end);
            }

            public void Visit(RoopUnitOperation op)
            {
                int begin = Builder.Count;
                Builder.Add(default(LowLevelOperation));    // Dummy
                EmitOperations(op.Operations, op.PtrChange);
                int end = Builder.Count;
                Builder.Add(new LowLevelOperation(Opcode.BrTrue, src: op.Src.Offset, value: begin));

                // Correct jump address
                Builder[begin] = new LowLevelOperation(Opcode.BrFalse, src: op.Src.Offset, value: end);
            }

            public void EmitOperations(ImmutableArray<IOperation> operations, int ptrChange)
            {
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
                    Builder.Add(new LowLevelOperation(Opcode.AddPtr, value: ptrChange));
                }
            }

            public void Visit(AssignOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.Assign, dest: op.Dest.Offset, value: op.Value));
            }

            public void Visit(AddAssignOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.AddAssign, dest: op.Dest.Offset, value: op.Value));
            }

            public void Visit(MultAddAssignOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.MultAddAssign, dest: op.Dest.Offset, src: op.Src.Offset, value: op.Value));
            }

            public void Visit(PutOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.Put, src: op.Src.Offset));
            }

            public void Visit(ReadOperation op)
            {
                Builder.Add(new LowLevelOperation(Opcode.Read, dest: op.Dest.Offset));
            }
        }
    }
}
