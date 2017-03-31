using Brainfuck.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Brainfuck.Core.LowLevel
{
    public static class LowLevelConverter
    {
        public static ImmutableArray<LowLevelOperation> ToLowLevel(this IOperation operation, Setting setting)
        {
            var visitor = new Visitor(setting);
            operation.Accept(visitor);

            IReadOnlyList<LowLevelOperation> list = visitor.list;
            if (setting.UseDynamicBuffer)
            {
                list = AddEnsureBufferCode(list);
            }

            return list.Concat(new[] { new LowLevelOperation(Opcode.Return) }).ToImmutableArray();
        }

        private static IReadOnlyList<LowLevelOperation> AddEnsureBufferCode(IReadOnlyList<LowLevelOperation> list)
        {
            var result = new List<LowLevelOperation>();
            var delayed = new List<(LowLevelOperation operation, int originalIndex)>();
            int offset = 0, maxOffsetDiff = 0;
            var addressMap = new Dictionary<int, int>();

            void EmitDelayedOperations()
            {
                if (maxOffsetDiff > 0)
                {
                    result.Add(new LowLevelOperation(Opcode.EnsureBuffer, value: (short)maxOffsetDiff));
                }

                foreach (var t in delayed)
                {
                    addressMap[t.originalIndex] = result.Count;
                    result.Add(t.operation);
                }

                delayed.Clear();
                offset = 0;
                maxOffsetDiff = 0;
            }

            for (int i = 0; i < list.Count; i++)
            {
                LowLevelOperation op = list[i];

                if (op.Opcode == Opcode.BrTrue || op.Opcode == Opcode.BrFalse)
                {
                    EmitDelayedOperations();
                }

                delayed.Add((op, i));
                maxOffsetDiff = Math.Max(maxOffsetDiff, offset + Math.Max(op.Src, op.Dest));

                if (op.Opcode == Opcode.AddPtr)
                {
                    offset += op.Value;
                    maxOffsetDiff = Math.Max(maxOffsetDiff, offset);
                }
                else if (op.Opcode == Opcode.BrTrue || op.Opcode == Opcode.BrFalse)
                {
                    EmitDelayedOperations();
                }
            }

            EmitDelayedOperations();

            // Correct jump addresses
            for (int i = 0; i < result.Count; i++)
            {
                switch (result[i].Opcode)
                {
                    case Opcode.BrTrue:
                    case Opcode.BrFalse:
                        result[i] = new LowLevelOperation(result[i].Opcode,
                                                            src: result[i].Src,
                                                            dest: result[i].Dest,
                                                            value: (short)addressMap[result[i].Value]);
                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        private class Visitor : IVisitor
        {
            public List<LowLevelOperation> list = new List<LowLevelOperation>();
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
                int begin = list.Count;
                list.Add(default(LowLevelOperation));    // Dummy
                EmitOperations(op.Operations, op.PtrChange);
                int end = list.Count - 1;

                // Correct jump address
                list[begin] = new LowLevelOperation(Opcode.BrFalse, src: (short)op.Src.Offset, value: (short)end);
            }

            public void Visit(RoopUnitOperation op)
            {
                int begin = list.Count;
                list.Add(default(LowLevelOperation));    // Dummy
                EmitOperations(op.Operations, op.PtrChange);
                int end = list.Count;
                list.Add(new LowLevelOperation(Opcode.BrTrue, src: (short)op.Src.Offset, value: (short)begin));

                // Correct jump address
                list[begin] = new LowLevelOperation(Opcode.BrFalse, src: (short)op.Src.Offset, value: (short)end);
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
                    list.Add(new LowLevelOperation(Opcode.AddPtr, value: (short)ptrChange));
                }
            }

            public void Visit(AssignOperation op)
            {
                list.Add(new LowLevelOperation(Opcode.Assign, dest: (short)op.Dest.Offset, value: (short)op.Value));
            }

            public void Visit(AddAssignOperation op)
            {
                list.Add(new LowLevelOperation(Opcode.AddAssign, dest: (short)op.Dest.Offset, value: (short)op.Value));
            }

            public void Visit(MultAddAssignOperation op)
            {
                list.Add(new LowLevelOperation(Opcode.MultAddAssign, dest: (short)op.Dest.Offset, src: (short)op.Src.Offset, value: (short)op.Value));
            }

            public void Visit(PutOperation op)
            {
                list.Add(new LowLevelOperation(Opcode.Put, src: (short)op.Src.Offset));
            }

            public void Visit(ReadOperation op)
            {
                list.Add(new LowLevelOperation(Opcode.Read, dest: (short)op.Dest.Offset));
            }
        }
    }
}
