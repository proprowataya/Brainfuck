using System;
using System.Collections.Generic;
using System.Text;

namespace Brainfuck.Core.Syntax
{
    public interface IVisitor
    {
        void Visit(BlockUnit node);
        void Visit(IfTrueUnit node);
        void Visit(RoopUnit node);
        void Visit(AssignStatement node);
        void Visit(PutStatement node);
        void Visit(ConstExpression node);
        void Visit(MemoryAccessExpression node);
        void Visit(GetExpression node);
        void Visit(AddExpression node);
        void Visit(MultiplyExpression node);
    }
}
